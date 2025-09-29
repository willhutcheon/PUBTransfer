//using Android.Content;
//using Android.Media;
//using Android.OS;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace PUBTransfer.Platforms.Android
//{
//    public class BuzzAndDing
//    {
//        private MediaPlayer mediaPlayer;
//        private Vibrator vibrator;
//        private readonly Context context;

//        public BuzzAndDing()
//        {
//            context = Application.Context;
//            mediaPlayer = MediaPlayer.Create(context, Resource.Raw.ding);
//            vibrator = (Vibrator)context.GetSystemService(Context.VibratorService);
//        }

//        public void Ding()
//        {
//            // Play sound
//            mediaPlayer?.Start();

//            // Vibrate
//            long[] pattern = { 0, 200, 100, 200 };
//            int[] amplitudes = { 0, 255, 0, 255 };
//            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
//            {
//                var effect = VibrationEffect.CreateWaveform(pattern, amplitudes, -1);
//                vibrator?.Vibrate(effect);
//            }
//            else
//            {
//                vibrator?.Vibrate(pattern, -1);
//            }
//        }

//        public void Release()
//        {
//            mediaPlayer?.Release();
//            mediaPlayer = null;
//        }
//    }
//}

#if ANDROID
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;

namespace PUBTransfer.Platforms.Android
{
    public class BuzzAndDing
    {
        private MediaPlayer mediaPlayer;
        private Vibrator vibrator;
        private readonly Context context;

        public BuzzAndDing(Context context)
        {
            //context = Android.App.Application.Context;
            mediaPlayer = MediaPlayer.Create(context, Resource.Raw.ding);
            vibrator = (Vibrator)context.GetSystemService(Context.VibratorService);
        }

        public void Ding()
        {
            // Play sound
            mediaPlayer?.Start();

            // Vibrate
            long[] pattern = { 0, 200, 100, 200 };
            int[] amplitudes = { 0, 255, 0, 255 };

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var effect = VibrationEffect.CreateWaveform(pattern, amplitudes, -1);
                vibrator?.Vibrate(effect);
            }
            else
            {
                vibrator?.Vibrate(pattern, -1);
            }
        }

        public void Release()
        {
            mediaPlayer?.Release();
            mediaPlayer = null;
        }
    }
}
#endif
