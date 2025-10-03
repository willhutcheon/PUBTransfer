//#if ANDROID
//using Android.App;
//using Android.Content;
//using Android.Media;
//using Android.OS;

//namespace PUBTransfer.Platforms.Android
//{
//    public class BuzzAndDing
//    {
//        private MediaPlayer mediaPlayer;
//        private Vibrator vibrator;
//        private readonly Context context;
//        public BuzzAndDing(Context context)
//        {
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
//#endif


#if ANDROID
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;

namespace PUBTransfer.Platforms.Android
{
    public class BuzzAndDing
    {
        private readonly Context context;
        private const string CHANNEL_ID = "pubtransfer_channel";
        public BuzzAndDing(Context context)
        {
            this.context = context;
            // Create notification channel (Android 8+)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var name = "PUBTransfer Notifications";
                var description = "Notifications for puff data upload";
                //var importance = NotificationImportance.Default;
                var importance = NotificationImportance.High;
                var channel = new NotificationChannel(CHANNEL_ID, name, importance)
                {
                    Description = description
                };

                var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }
        public void ShowNotification(string title, string message)
        {
            // Build notification
            //var builder = new NotificationCompat.Builder(context, CHANNEL_ID)
            //              .SetPriority((int)NotificationPriority.High)
            //              .SetCategory(NotificationCompat.CategoryMessage)
            //              .SetDefaults((int)(NotificationDefaults.Sound | NotificationDefaults.Vibrate))
            //              .SetVisibility((int)NotificationVisibility.Public)
            //              .SetContentTitle(title)
            //              .SetContentText(message)
            //              .SetSmallIcon(Resource.Drawable.icon)
            //              .SetPriority((int)NotificationPriority.High)
            //              .SetAutoCancel(true)
            //              .SetDefaults((int)(NotificationDefaults.Sound | NotificationDefaults.Vibrate));
            //var notificationManager = NotificationManagerCompat.From(context);
            //notificationManager.Notify(new System.Random().Next(), builder.Build());

            var builder = new NotificationCompat.Builder(context, CHANNEL_ID)
                          .SetContentTitle("Upload Complete")
                          .SetContentText("Puff data sent to Event Hub!")
                          .SetSmallIcon(Resource.Drawable.icon)
                          .SetPriority((int)NotificationPriority.High)
                          .SetDefaults((int)(NotificationDefaults.Sound | NotificationDefaults.Vibrate))
                          .SetAutoCancel(true);
            var manager = NotificationManagerCompat.From(context);
            manager.Notify(1, builder.Build());
        }
        // Optional: Keep existing Ding method for immediate sound+vibration
        public void Ding()
        {
            ShowNotification("Upload Complete", "Puff data sent to Event Hub!");
        }
    }
}
#endif