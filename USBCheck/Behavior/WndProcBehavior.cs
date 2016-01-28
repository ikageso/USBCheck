using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interactivity;
using System.Windows;
using System.Windows.Input;
using System.Configuration;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using USBCheck.View;
using static  USBCheck.Common.Win32Helper;

namespace USBCheck.Behavior
{
    /// <summary>
    /// USBの抜き差しをWndProcで受け取るBehavior
    /// </summary>
    public class WndProcBehavior : Behavior<MainWindow>
    {
        private string _DescriptionUSB;     // 対象のUSBのDescription

        #region プロパティ
        public static readonly DependencyProperty DetectedUSBProperty =
            DependencyProperty.Register("DetectedUSB", typeof(bool), typeof(WndProcBehavior), new PropertyMetadata(null));

        /// <summary>
        /// DetectedUSB
        /// </summary>
        public bool DetectedUSB
        {
            get { return (bool)this.GetValue(DetectedUSBProperty); }
            set { this.SetValue(DetectedUSBProperty, value); }
        }
        #endregion

        /// <summary>
        /// OnAttached
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Loaded += new RoutedEventHandler(AssociatedObject_Loaded);
        }

        /// <summary>
        /// AssociatedObject_Loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            // WindProcを設定
            IntPtr handle = new WindowInteropHelper(this.AssociatedObject).Handle;
            HwndSource source = HwndSource.FromHwnd(handle); 
            source.AddHook(new HwndSourceHook(WndProc));

            // USBデバイスの抜き差しを取得する設定
            DevBroadcastDeviceinterface dbi = new DevBroadcastDeviceinterface
            {
                DeviceType = DbtDevtypDeviceinterface,
                Reserved = 0,
                ClassGuid = GuidDevinterfaceUSBDevice,
                Name = 0
            };

            dbi.Size = Marshal.SizeOf(dbi);
            IntPtr buffer = Marshal.AllocHGlobal(dbi.Size);
            Marshal.StructureToPtr(dbi, buffer, true);

            // デバイス変更通知を受け取れるように登録
            RegisterDeviceNotification(handle, buffer, 0);

            // 対象のUSBのDescriptionを取得
            _DescriptionUSB = ConfigurationManager.AppSettings["TargetDescription"];

            // デバイス一覧取得
            var devList = MakeDeviceList();

            // 対象のUSBは接続されているか？
            DetectedUSB = devList.Any(x => x.Contains(_DescriptionUSB));
        }

        /// <summary>
        /// OnDetaching
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            this.AssociatedObject.Loaded -= new RoutedEventHandler(AssociatedObject_Loaded);

        }

        /// <summary>
        /// WndProc
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DEVICECHANGE:

                    Debug.WriteLine("WM_DEVICECHANGE wParam={0}({2:x}), lParam={1}({3:x})", wParam, lParam, wParam, lParam);

                    // 接続した
                    if (wParam.ToInt32() == DBT_DEVICEARRIVAL)
                    {
                        // デバイス一覧取得
                        var devList = MakeDeviceList();

                        // 対象のUSBは接続されているか？
                        if (!DetectedUSB && devList.Any(x => x.Contains(_DescriptionUSB)))
                        {
                            Debug.WriteLine("接続:" + _DescriptionUSB);

                            DetectedUSB = true;
                        }

                    }
                    // 切断した
                    else if (wParam.ToInt32() == DBT_DEVICEREMOVECOMPLETE)
                    {
                        var devList = MakeDeviceList();

                        // 対象のUSBは接続されているか？
                        if (!devList.Any(x => x.Contains(_DescriptionUSB)))
                        {
                            Debug.WriteLine("切断:" + _DescriptionUSB);

                            DetectedUSB = false;
                        }
                    }

                    break;
            }

            return IntPtr.Zero;
        }


    }
}
