using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Reactive.Bindings;

namespace FaceKirby
{
    public class AsyncKinectManager
    {
        ReactiveProperty<KinectSensor> _Sensor = new ReactiveProperty<KinectSensor>();
        public IReadOnlyReactiveProperty<KinectSensor> Sensor => _Sensor;
        KinectSensor _sensorCache;

        // アプリケーション起動時に既にデバイスが接続されている場合も発生します。
        public ReadOnlyReactiveProperty<KinectSensor> SensorConnected { get; }
        public ReadOnlyReactiveProperty<KinectSensor> SensorDisconnected { get; }

        public AsyncKinectManager()
        {
            SensorDisconnected = _Sensor
                .Select(s => _sensorCache)
                .Where(s => s != null)
                .ToReadOnlyReactiveProperty(null, ReactivePropertyMode.None);
            SensorConnected = _Sensor
                .Do(s => _sensorCache = s)
                .Where(s => s != null)
                .ToReadOnlyReactiveProperty(null, ReactivePropertyMode.None);
        }

        // SensorConnected を購読した後で呼び出してください。
        public void Initialize()
        {
            Task.Run(() =>
            {
                KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
                _Sensor.Value = FindSensor();
            });
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (Sensor.Value == null)
            {
                if (e.Status == KinectStatus.Connected)
                {
                    _Sensor.Value = e.Sensor;
                }
            }
            else if (Sensor.Value == e.Sensor)
            {
                if (e.Status != KinectStatus.Connected)
                {
                    _Sensor.Value = FindSensor();
                }
            }
        }

        static KinectSensor FindSensor()
        {
            return KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
        }
    }
}
