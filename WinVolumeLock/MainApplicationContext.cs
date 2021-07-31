using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Observables;

namespace WinVolumeLock
{
    internal class MainApplicationContext : ApplicationContext
    {
        private readonly System.ComponentModel.IContainer components;
        private readonly NotifyIcon icon;
        private readonly ToolStripMenuItem mi_volumeMax;
        private readonly ToolStripMenuItem mi_volumeSet;
        private readonly ToolStripMenuItem mi_password;
        private readonly ToolStripMenuItem mi_exit;
        private readonly ToolStripMenuItem mi_lock;
        private readonly IAudioController audioController;

        private DateTime lastNotifyOfLockTime;

        /// <summary>
        /// Lock or unlock the volume control.
        /// </summary>
        public bool VolLock
        {
            get
            {
                return _volLock;
            }

            set
            {
                if (value)
                {
                    VolAtLockTime = audioController.DefaultPlaybackDevice.Volume;
                }

                _volLock = value;
            }
        }

        private bool _volLock;

        /// <summary>
        /// The volume level the system was set to when the volume gets locked.
        /// </summary>
        public double VolAtLockTime { get; set; }

        /// <summary>
        /// Initialize a new instance of <see cref="MainApplicationContext()"/>
        /// </summary>
        internal MainApplicationContext()
        {
            components = new System.ComponentModel.Container();
            audioController = new CoreAudioController();
            VolLock = Properties.Settings.Default.IsLocked;
            VolAtLockTime = Properties.Settings.Default.LastVolLevel;
            lastNotifyOfLockTime = DateTime.Now.AddMinutes(-1);

            //
            // mi_volumeMax
            //
            mi_volumeMax = new ToolStripMenuItem("Volume Max")
            {
                ToolTipText = "Prevent the volume from going higher than the set value.",
                AutoToolTip = true,
                Checked = Properties.Settings.Default.VolMax
            };
            mi_volumeMax.Click += VolumeMax_MenuItem_Click;

            //
            // mi_volumeSet
            //
            mi_volumeSet = new ToolStripMenuItem("Volume Set")
            {
                ToolTipText = "Lock the volume to the set value.",
                AutoToolTip = true,
                Checked = !Properties.Settings.Default.VolMax
            };
            mi_volumeSet.Click += VolumeSet_MenuItem_Click;

            //
            // mi_password
            //
            mi_password = new ToolStripMenuItem("Password...");
            mi_password.Click += SetChangePassword_MenuItem_Click;

            //
            // mi_exit
            //
            mi_exit = new ToolStripMenuItem("Exit")
            {
                ToolTipText = "Exit the program.",
                AutoToolTip = true
            };
            mi_exit.Click += Exit_MenuItem_Click;

            //
            // mi_lock
            //
            mi_lock = new ToolStripMenuItem("Lock")
            {
                ToolTipText = "Lock or unlock the volume control.",
                AutoToolTip = true
            };

            mi_lock.Click += LockUnlock_MenuItem_Click;

            //
            // icon
            //
            icon = new NotifyIcon(components)
            {
                Text = "WinVolumeLock",
                Icon = Properties.Resources.speaker_unlocked,
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip(components)
            };

            icon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                mi_volumeMax,
                mi_volumeSet,
                //mi_password,
                new ToolStripSeparator(),
                mi_lock,
                new ToolStripSeparator(),
                mi_exit
            });

            icon.ContextMenuStrip.Opening += MainContextMenuStrip_Opening;

            audioController.DefaultPlaybackDevice.VolumeChanged.When(UpdateVolume);

            UpdateVolume(null);
        }

        private void MaybeNotify()
        {
            if (DateTime.Now.Subtract(lastNotifyOfLockTime).TotalMinutes >= 1)
            {
                lastNotifyOfLockTime = DateTime.Now;
                icon.ShowBalloonTip(1000, "WinVolLock", "The volume control is locked.", ToolTipIcon.Info);
            }
        }

        private bool UpdateVolume(DeviceVolumeChangedArgs args)
        {
            if (VolLock)
            {
                double volVal = args == null ? audioController.DefaultPlaybackDevice.Volume : args.Volume;
                if (mi_volumeMax.Checked)
                {
                    if (volVal > VolAtLockTime)
                    {
                        audioController.DefaultPlaybackDevice.SetVolumeAsync(VolAtLockTime).Wait();
                        MaybeNotify();
                    }
                }
                else if (mi_volumeSet.Checked)
                {
                    audioController.DefaultPlaybackDevice.SetVolumeAsync(VolAtLockTime).Wait();
                    MaybeNotify();
                }
            }

            return true;
        }

        private void Exit_MenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.IsLocked = false;
            Properties.Settings.Default.Save();

            Application.Exit();
        }

        private void MainContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mi_volumeMax.Enabled = !VolLock;
            mi_volumeSet.Enabled = !VolLock;
            mi_password.Enabled = !VolLock;
            mi_exit.Enabled = !VolLock;
            mi_lock.Text = VolLock ? "Unlock Volume" : "Lock Volume";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void VolumeMax_MenuItem_Click(object sender, EventArgs e)
        {
            mi_volumeSet.Checked = false;
            mi_volumeMax.Checked = true;
        }

        private void VolumeSet_MenuItem_Click(object sender, EventArgs e)
        {
            mi_volumeMax.Checked = false;
            mi_volumeSet.Checked = true;
        }

        private void SetChangePassword_MenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void LockUnlock_MenuItem_Click(object sender, EventArgs e)
        {
            if (VolLock)
            {
                Point p = Cursor.Position;

                if (Program.GetUacPrivs())
                {
                    VolLock = false;
                    icon.ContextMenuStrip.Show(p);
                }
            }
            else
            {
                VolLock = true;

                Properties.Settings.Default.LastVolLevel = VolAtLockTime;
                Properties.Settings.Default.VolMax = mi_volumeMax.Checked;
                Properties.Settings.Default.IsLocked = true;
                Properties.Settings.Default.Save();
            }
        }
    }
}
