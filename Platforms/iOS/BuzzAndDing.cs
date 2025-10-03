//#if IOS
//using AudioToolbox;
//using AVFoundation;
//using Foundation;
//using UIKit;

//namespace PUBTransfer.Platforms.iOS
//{
//    public class BuzzAndDing
//    {
//        private AVAudioPlayer player;
//        public BuzzAndDing()
//        {
//            // Configure audio session
//            var session = AVAudioSession.SharedInstance();
//            session.SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.MixWithOthers);
//            session.SetActive(true);
//            // Load sound
//            var url = NSBundle.MainBundle.GetUrlForResource("ding", "wav");
//            if (url != null)
//            {
//                player = AVAudioPlayer.FromUrl(url);
//                player.PrepareToPlay();
//            }
//        }
//        public void Ding()
//        {
//            player?.Play();
//            UIDevice.CurrentDevice.PlayInputClick(); // light haptic
//            SystemSound.Vibrate.PlaySystemSound();   // default vibration
//        }
//        public void Release()
//        {
//            player?.Dispose();
//            player = null;
//        }
//    }
//}
//#endif


//#if IOS
//using AudioToolbox;
//using AVFoundation;
//using Foundation;
//using UIKit;
//using UserNotifications;

//namespace PUBTransfer.Platforms.iOS
//{
//    public class NotificationHelper
//    {
//        private AVAudioPlayer player;

//        public NotificationHelper()
//        {
//            // Load custom sound if available
//            var url = NSBundle.MainBundle.GetUrlForResource("ding", "wav");
//            if (url != null)
//            {
//                player = AVAudioPlayer.FromUrl(url);
//                player.PrepareToPlay();
//            }
//        }

//        /// <summary>
//        /// Request notification permissions
//        /// </summary>
//        public async System.Threading.Tasks.Task RequestPermissionsAsync()
//        {
//            var center = UNUserNotificationCenter.Current;
//            var (granted, error) = await center.RequestAuthorizationAsync(
//                UNAuthorizationOptions.Alert |
//                UNAuthorizationOptions.Sound |
//                UNAuthorizationOptions.Badge
//            );

//            if (!granted)
//                Console.WriteLine("Notifications not allowed!");
//        }

//        /// <summary>
//        /// Show a banner notification with optional custom sound
//        /// </summary>
//        public void ShowNotification(string title, string message, bool useCustomSound = true)
//        {
//            // Play custom sound immediately (optional)
//            if (useCustomSound)
//                player?.Play();

//            // Vibrate / haptic feedback
//            UIDevice.CurrentDevice.PlayInputClick();
//            SystemSound.Vibrate.PlaySystemSound();

//            // Prepare notification content
//            var content = new UNMutableNotificationContent
//            {
//                Title = title,
//                Body = message,
//                Sound = useCustomSound && player != null
//                    ? UNNotificationSound.GetSound("ding.wav")
//                    : UNNotificationSound.Default
//            };

//            // Trigger immediately
//            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(0.5, false);

//            var request = UNNotificationRequest.FromIdentifier(
//                Guid.NewGuid().ToString(), content, trigger);

//            UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
//            {
//                if (err != null)
//                    Console.WriteLine($"Error scheduling notification: {err}");
//            });
//        }

//        public void Release()
//        {
//            player?.Dispose();
//            player = null;
//        }
//    }


//    public class BuzzAndDing
//    {
//        private AVAudioPlayer player;
//        public BuzzAndDing()
//        {
//            // Configure audio session
//            var session = AVAudioSession.SharedInstance();
//            session.SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.MixWithOthers);
//            session.SetActive(true);
//            // Load sound
//            var url = NSBundle.MainBundle.GetUrlForResource("ding", "wav");
//            if (url != null)
//            {
//                player = AVAudioPlayer.FromUrl(url);
//                player.PrepareToPlay();
//            }
//        }
//        public void Ding()
//        {
//            player?.Play();
//            UIDevice.CurrentDevice.PlayInputClick(); // light haptic
//            SystemSound.Vibrate.PlaySystemSound();   // default vibration
//        }
//        public void Release()
//        {
//            player?.Dispose();
//            player = null;
//        }
//    }
//}
//#endif



//#if IOS
//using AudioToolbox;
//using AVFoundation;
//using Foundation;
//using UIKit;
//using UserNotifications;
//using System;
//using System.Threading.Tasks;

//namespace PUBTransfer.Platforms.iOS
//{
//    public class NotificationHelper
//    {
//        private AVAudioPlayer player;
//        private bool permissionsGranted = false;

//        public NotificationHelper()
//        {
//            // Load custom sound from main bundle
//            var url = NSBundle.MainBundle.GetUrlForResource("ding", "wav");
//            if (url != null)
//            {
//                player = AVAudioPlayer.FromUrl(url);
//                player.PrepareToPlay();
//            }
//        }

//        /// <summary>
//        /// Request notification permissions once at app startup
//        /// </summary>
//        public async Task RequestPermissionsAsync()
//        {
//            if (permissionsGranted) return;

//            var center = UNUserNotificationCenter.Current;
//            var (granted, error) = await center.RequestAuthorizationAsync(
//                UNAuthorizationOptions.Alert |
//                UNAuthorizationOptions.Sound |
//                UNAuthorizationOptions.Badge
//            );

//            permissionsGranted = granted;

//            if (!granted)
//                Console.WriteLine("Notifications not allowed!");
//        }

//        /// <summary>
//        /// Show immediate banner, sound, and vibration
//        /// </summary>
//        public void ShowNotification(string title, string message)
//        {
//            if (!permissionsGranted)
//            {
//                Console.WriteLine("Notification permission not granted.");
//                return;
//            }

//            // Play sound immediately
//            player?.Play();

//            // Haptic feedback & vibration
//            UIDevice.CurrentDevice.PlayInputClick();
//            SystemSound.Vibrate.PlaySystemSound();

//            // Prepare notification content
//            var content = new UNMutableNotificationContent
//            {
//                Title = title,
//                Body = message,
//                Sound = UNNotificationSound.GetSound("ding.wav") // use custom sound
//            };

//            // Trigger immediately
//            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(0, false);

//            var request = UNNotificationRequest.FromIdentifier(
//                Guid.NewGuid().ToString(), content, trigger);

//            UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
//            {
//                if (err != null)
//                    Console.WriteLine($"Error scheduling notification: {err}");
//            });
//        }

//        public void Release()
//        {
//            player?.Dispose();
//            player = null;
//        }
//    }
//}
//#endif


#if IOS
using AudioToolbox;
using AVFoundation;
using Foundation;
using UIKit;
using UserNotifications;
using System;
using System.Threading.Tasks;

namespace PUBTransfer.Platforms.iOS
{
    public class NotificationHelper : UNUserNotificationCenterDelegate
    {
        private AVAudioPlayer player;
        private bool permissionsGranted = false;
        public NotificationHelper()
        {
            // Set this instance as the delegate
            UNUserNotificationCenter.Current.Delegate = this;
            // Load custom sound from main bundle
            var url = NSBundle.MainBundle.GetUrlForResource("ding", "wav");
            if (url != null)
            {
                player = AVAudioPlayer.FromUrl(url);
                player.PrepareToPlay();
            }
        }
        /// <summary>
        /// This method allows notifications to show as banners even when app is in foreground
        /// </summary>
        [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
        public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            // Show banner, sound, and badge even in foreground
            completionHandler(UNNotificationPresentationOptions.Banner |
                            UNNotificationPresentationOptions.Sound |
                            UNNotificationPresentationOptions.Badge);
        }
        public async Task RequestPermissionsAsync()
        {
            if (permissionsGranted) return;

            var center = UNUserNotificationCenter.Current;
            var (granted, error) = await center.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Sound |
                UNAuthorizationOptions.Badge
            );
            permissionsGranted = granted;
            if (!granted)
                Console.WriteLine("Notifications not allowed!");
        }
        public void ShowNotification(string title, string message)
        {
            if (!permissionsGranted)
            {
                Console.WriteLine("Notification permission not granted.");
                return;
            }
            // Play sound immediately
            player?.Play();
            // Haptic feedback & vibration
            UIDevice.CurrentDevice.PlayInputClick();
            SystemSound.Vibrate.PlaySystemSound();
            // Prepare notification content
            var content = new UNMutableNotificationContent
            {
                Title = title,
                Body = message,
                Sound = UNNotificationSound.GetSound("ding.wav")
            };
            // Trigger immediately
            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(0.1, false);
            var request = UNNotificationRequest.FromIdentifier(
                Guid.NewGuid().ToString(), content, trigger);
            UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
            {
                if (err != null)
                    Console.WriteLine($"Error scheduling notification: {err}");
            });
        }
        public void Release()
        {
            player?.Dispose();
            player = null;
        }
    }
}
#endif