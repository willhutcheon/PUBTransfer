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

        public BuzzAndDing()
        {
            // Load the sound file from app bundle
            var url = NSBundle.MainBundle.GetUrlForResource("ding", "mp3");
            if (url != null)
            {
                player = AVAudioPlayer.FromUrl(url);
                player.PrepareToPlay();
            }
        }

        public void Ding()
        {
            // Play custom sound
            player?.Play();

            // Vibrate / Haptic feedback
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
