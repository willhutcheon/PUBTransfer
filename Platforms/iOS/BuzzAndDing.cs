#if IOS
using AudioToolbox;
using AVFoundation;
using Foundation;
using UIKit;

namespace PUBTransfer.Platforms.iOS
{
    public class BuzzAndDing
    {
        private AVAudioPlayer player;
        //public BuzzAndDing()
        //{
        //    // Load the sound file from app bundle
        //    //var url = NSBundle.MainBundle.GetUrlForResource("ding", "mp3");
        //    var url = NSBundle.MainBundle.GetUrlForResource("ding", "wav");
        //    if (url != null)
        //    {
        //        player = AVAudioPlayer.FromUrl(url);
        //        player.PrepareToPlay();
        //    }
        //}
        //public void Ding()
        //{
        //    // Play custom sound
        //    player?.Play();
        //    // Vibrate / Haptic feedback
        //    UIDevice.CurrentDevice.PlayInputClick(); // light haptic
        //    SystemSound.Vibrate.PlaySystemSound();   // default vibration
        //}

        public BuzzAndDing()
        {
            // Configure audio session
            var session = AVAudioSession.SharedInstance();
            session.SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.MixWithOthers);
            session.SetActive(true);

            // Load sound
            var url = NSBundle.MainBundle.GetUrlForResource("ding", "wav");
            if (url != null)
            {
                player = AVAudioPlayer.FromUrl(url);
                player.PrepareToPlay();
            }
        }

        public void Ding()
        {
            player?.Play();
            UIDevice.CurrentDevice.PlayInputClick(); // light haptic
            SystemSound.Vibrate.PlaySystemSound();   // default vibration
        }

        public void Release()
        {
            player?.Dispose();
            player = null;
        }
    }
}
#endif
