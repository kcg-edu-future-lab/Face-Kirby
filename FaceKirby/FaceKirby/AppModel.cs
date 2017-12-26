using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media.Media3D;
using FaceKirby.Properties;
using Microsoft.Kinect;
using Reactive.Bindings;

namespace FaceKirby
{
    public class AppModel
    {
        public static readonly TimeSpan FramesInterval = TimeSpan.FromSeconds(1 / 30.0);

        static readonly Func<Vector3D, bool, bool> GetIsTarget = (v, isTargetPreviously) => isTargetPreviously ? v.Z < Settings.Default.BodyRange_Out_Z : v.Z < Settings.Default.BodyRange_In_Z;

        public AsyncKinectManager KinectManager { get; } = new AsyncKinectManager();

        public ReadOnlyReactiveProperty<Skeleton> TargetBody { get; }
        public ReadOnlyReactiveProperty<bool> HasTargetBody { get; }

        public ReadOnlyReactiveProperty<JointInfo?> HitHand { get; }
        public ReadOnlyReactiveProperty<bool> IsHandHit { get; }

        public AppModel()
        {
            KinectManager.SensorConnected
                .Subscribe(sensor =>
                {
                    sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                    sensor.SkeletonStream.EnableWithDefaultSmoothing();

                    try
                    {
                        sensor.Start();
                    }
                    catch (Exception ex)
                    {
                        // センサーが他のプロセスに既に使用されている場合に発生します。
                        Debug.WriteLine(ex);
                    }
                });
            KinectManager.SensorDisconnected
                .Subscribe(sensor => sensor.Stop());
            KinectManager.Initialize();

            // フレームのデータ。
            var frameData = Observable.Interval(FramesInterval)
                .Select(_ => new
                {
                    Sensor = KinectManager.Sensor.Value,
                    ColorData = KinectManager.Sensor.Value.GetColorData(FramesInterval),
                    DepthData = KinectManager.Sensor.Value.GetDepthData(FramesInterval),
                    BodyData = KinectManager.Sensor.Value.GetSkeletonData(FramesInterval),
                })
                .ToReadOnlyReactiveProperty(null, ReactivePropertyMode.DistinctUntilChanged);

            // ターゲットの人物。
            TargetBody = frameData
                .Where(_ => _.BodyData != null)
                .Select(_ => GetTargetBody(_.BodyData, TargetBody.Value))
                .ToReadOnlyReactiveProperty(null, ReactivePropertyMode.DistinctUntilChanged);

            // ターゲットの人物が存在するかどうか。
            HasTargetBody = TargetBody
                .Select(b => b != null)
                .ToReadOnlyReactiveProperty();

            // ヒット ゾーン内の手。z 要素は体からの差分。
            HitHand = TargetBody
                .Select(b => GetHitHand(b, HitHand.Value))
                .ToReadOnlyReactiveProperty(null, ReactivePropertyMode.DistinctUntilChanged);

            // ヒット ゾーン内かどうか。
            IsHandHit = HitHand
                .Select(_ => _.HasValue)
                .ToReadOnlyReactiveProperty();
        }

        static Skeleton GetTargetBody(Skeleton[] bodyData, Skeleton oldBody)
        {
            if (bodyData.Length == 0) return null;

            if (oldBody != null)
            {
                var body = bodyData.FirstOrDefault(b => b.TrackingId == oldBody.TrackingId);

                return (body != null && body.TrackingState == SkeletonTrackingState.Tracked && GetIsTarget(body.Position.ToVector3D(), true))
                    ? body
                    : GetForwardBody(bodyData);
            }
            else
            {
                return GetForwardBody(bodyData);
            }
        }

        static Skeleton GetForwardBody(Skeleton[] bodyData)
        {
            return bodyData
                .Where(b => b.TrackingState == SkeletonTrackingState.Tracked)
                .OrderBy(b => b.Position.Z)
                .Where(b => GetIsTarget(b.Position.ToVector3D(), false))
                .FirstOrDefault();
        }

        static JointInfo? GetHitHand(Skeleton body, JointInfo? oldInfo)
        {
            if (body == null) return null;

            Joint hand;

            // 前回と同じ手が範囲内に存在しない場合、null を返します。
            if (oldInfo.HasValue)
            {
                if (body.TrackingId != oldInfo.Value.BodyId) return null;

                hand = body.Joints[oldInfo.Value.JointType];
                if (hand.TrackingState == JointTrackingState.NotTracked) return null;
            }
            else
            {
                hand = new[] { body.Joints[JointType.HandLeft], body.Joints[JointType.HandRight] }
                    .Where(j => j.TrackingState != JointTrackingState.NotTracked)
                    .OrderBy(j => j.Position.Z)
                    .FirstOrDefault();
                if (hand.Position.Z == 0) return null;
            }

            if (!GetIsHandHit(body, hand, oldInfo.HasValue)) return null;

            // 返される z 要素は体からの差分です。
            return new JointInfo
            {
                BodyId = body.TrackingId,
                JointType = hand.JointType,
                Position = hand.Position.ToVector3D() - new Vector3D(0, 0, body.Position.Z),
            };
        }

        static bool GetIsHandHit(Skeleton body, Joint hand, bool isHitPreviously)
        {
            var handForward = hand.Position.Z - body.Position.Z;
            return handForward < -0.3;
        }
    }

    public struct JointInfo
    {
        public int BodyId { get; set; }
        public JointType JointType { get; set; }
        public Vector3D Position { get; set; }
    }
}
