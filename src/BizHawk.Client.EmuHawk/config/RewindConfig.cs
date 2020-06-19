﻿using System;
using System.Windows.Forms;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RewindConfig : Form
	{
		private readonly MainForm _mainForm;
		private readonly Config _config;
		private readonly IStatable _statableCore;

		private long _stateSize;
		private int _stateSizeCategory = 1; // 1 = small, 2 = med, 3 = large // TODO: enum

		public RewindConfig(MainForm mainForm, Config config, IStatable statableCore)
		{
			_mainForm = mainForm;
			_config = config;
			_statableCore = statableCore;
			InitializeComponent();
		}

		private void RewindConfig_Load(object sender, EventArgs e)
		{
			if (_mainForm.Rewinder?.Active == true)
			{
				FullnessLabel.Text = $"{_mainForm.Rewinder.FullnessRatio * 100:0.00}%";
				RewindFramesUsedLabel.Text = _mainForm.Rewinder.Count.ToString();
			}
			else
			{
				FullnessLabel.Text = "N/A";
				RewindFramesUsedLabel.Text = "N/A";
			}

			RewindSpeedNumeric.Value = _config.Rewind.SpeedMultiplier;
			DiskBufferCheckbox.Checked = _config.Rewind.OnDisk;
			_stateSize = _statableCore.CloneSavestate().Length;
			BufferSizeUpDown.Value = Math.Max(_config.Rewind.BufferSize, BufferSizeUpDown.Minimum);

			UseCompression.Checked = _config.Rewind.UseCompression;

			SmallSavestateNumeric.Value = _config.Rewind.FrequencySmall;
			MediumSavestateNumeric.Value = _config.Rewind.FrequencyMedium;
			LargeSavestateNumeric.Value = _config.Rewind.FrequencyLarge;

			SmallStateEnabledBox.Checked = _config.Rewind.EnabledSmall;
			MediumStateEnabledBox.Checked = _config.Rewind.EnabledMedium;
			LargeStateEnabledBox.Checked = _config.Rewind.EnabledLarge;

			SetSmallEnabled();
			SetMediumEnabled();
			SetLargeEnabled();

			SetStateSize();

			nudCompression.Value = _config.Savestates.CompressionLevelNormal;

			rbStatesBinary.Checked = _config.Savestates.Type == SaveStateType.Binary;
			rbStatesText.Checked = _config.Savestates.Type == SaveStateType.Text;

			BackupSavestatesCheckbox.Checked = _config.Savestates.MakeBackups;
			ScreenshotInStatesCheckbox.Checked = _config.Savestates.SaveScreenshot;
			LowResLargeScreenshotsCheckbox.Checked = !_config.Savestates.NoLowResLargeScreenshots;
			BigScreenshotNumeric.Value = _config.Savestates.BigScreenshotSize / 1024;

			ScreenshotInStatesCheckbox_CheckedChanged(null, null);
		}

		private void ScreenshotInStatesCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			LowResLargeScreenshotsCheckbox.Enabled =
				BigScreenshotNumeric.Enabled =
				KbLabel.Enabled =
				ScreenshotInStatesCheckbox.Checked;
		}

		private string FormatKB(long n)
		{
			double num = n / 1024.0;

			if (num >= 1024)
			{
				num /= 1024.0;
				return $"{num:0.00} MB";
			}

			return $"{num:0.00} KB";
		}

		private void SetStateSize()
		{
			StateSizeLabel.Text = FormatKB(_stateSize);

			SmallLabel.Text = $"Small savestates";
			MediumLabel.Text = $"Medium savestates";
			LargeLabel.Text = $"Large savestates";

			_stateSizeCategory = 1;
			SmallLabel.Font = new Font(SmallLabel.Font, FontStyle.Italic);
			MediumLabel.Font = new Font(SmallLabel.Font, FontStyle.Regular);
			LargeLabel.Font = new Font(SmallLabel.Font, FontStyle.Regular);

			CalculateEstimates();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private bool TriggerRewindSettingsReload { get; set; }

		private T PutRewindSetting<T>(T setting, T value) where T : IEquatable<T>
		{
			if (setting.Equals(value))
			{
				return setting;
			}

			TriggerRewindSettingsReload = true;
			return value;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			// These settings are used by DoRewindSettings, which we'll only call if anything actually changed (i.e. preserve rewind history if possible)
			_config.Rewind.UseCompression = PutRewindSetting(_config.Rewind.UseCompression, UseCompression.Checked);
			_config.Rewind.EnabledSmall = PutRewindSetting(_config.Rewind.EnabledSmall, SmallStateEnabledBox.Checked);
			_config.Rewind.EnabledMedium = PutRewindSetting(_config.Rewind.EnabledMedium, MediumStateEnabledBox.Checked);
			_config.Rewind.EnabledLarge = PutRewindSetting(_config.Rewind.EnabledLarge, LargeStateEnabledBox.Checked);
			_config.Rewind.FrequencySmall = PutRewindSetting(_config.Rewind.FrequencySmall, (int)SmallSavestateNumeric.Value);
			_config.Rewind.FrequencyMedium = PutRewindSetting(_config.Rewind.FrequencyMedium, (int)MediumSavestateNumeric.Value);
			_config.Rewind.FrequencyLarge = PutRewindSetting(_config.Rewind.FrequencyLarge, (int)LargeSavestateNumeric.Value);
			_config.Rewind.BufferSize = PutRewindSetting(_config.Rewind.BufferSize, (int)BufferSizeUpDown.Value);
			_config.Rewind.OnDisk = PutRewindSetting(_config.Rewind.OnDisk, DiskBufferCheckbox.Checked);

			// These settings are not used by DoRewindSettings
			_config.Rewind.SpeedMultiplier = (int)RewindSpeedNumeric.Value;
			_config.Savestates.CompressionLevelNormal = (int)nudCompression.Value;
			if (rbStatesBinary.Checked) _config.Savestates.Type = SaveStateType.Binary;
			if (rbStatesText.Checked) _config.Savestates.Type = SaveStateType.Text;
			_config.Savestates.MakeBackups = BackupSavestatesCheckbox.Checked;
			_config.Savestates.SaveScreenshot = ScreenshotInStatesCheckbox.Checked;
			_config.Savestates.NoLowResLargeScreenshots = !LowResLargeScreenshotsCheckbox.Checked;
			_config.Savestates.BigScreenshotSize = (int)BigScreenshotNumeric.Value * 1024;

			if (TriggerRewindSettingsReload)
			{
				_mainForm.CreateRewinder();
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void SetSmallEnabled()
		{
			SmallLabel.Enabled = SmallLabel2.Enabled
				= SmallSavestateNumeric.Enabled = SmallLabel3.Enabled
				= SmallStateEnabledBox.Checked;
		}

		private void SetMediumEnabled()
		{
			MediumLabel.Enabled = MediumLabel2.Enabled
				= MediumSavestateNumeric.Enabled = MediumLabel3.Enabled
				= MediumStateEnabledBox.Checked;
		}

		private void SetLargeEnabled()
		{
			LargeLabel.Enabled = LargeLabel2.Enabled
				= LargeSavestateNumeric.Enabled = LargeLabel3.Enabled
				= LargeStateEnabledBox.Checked;
		}

		private void SmallStateEnabledBox_CheckStateChanged(object sender, EventArgs e)
		{
			SetSmallEnabled();
		}

		private void MediumStateEnabledBox_CheckStateChanged(object sender, EventArgs e)
		{
			SetMediumEnabled();
		}

		private void LargeStateEnabledBox_CheckStateChanged(object sender, EventArgs e)
		{
			SetLargeEnabled();
		}

		private void LargeLabel_Click(object sender, EventArgs e)
		{
			LargeStateEnabledBox.Checked ^= true;
		}

		private void MediumLabel_Click(object sender, EventArgs e)
		{
			MediumStateEnabledBox.Checked ^= true;
		}

		private void SmallLabel_Click(object sender, EventArgs e)
		{
			SmallStateEnabledBox.Checked ^= true;
		}

		private void CalculateEstimates()
		{
			long avgStateSize;

			if (UseCompression.Checked || _stateSize == 0)
			{
				if (_mainForm.Rewinder?.Count > 0)
				{
					avgStateSize = _mainForm.Rewinder.Size / _mainForm.Rewinder.Count;
				}
				else
				{
					avgStateSize = _stateSize;
				}
			}
			else
			{
				avgStateSize = _stateSize;
			}

			var bufferSize = (long)BufferSizeUpDown.Value;
			bufferSize *= 1024 * 1024;
			var estFrames = bufferSize / avgStateSize;

			long estFrequency = 0;
			switch (_stateSizeCategory)
			{
				case 1:
					estFrequency = (long)SmallSavestateNumeric.Value;
					break;
				case 2:
					estFrequency = (long)MediumSavestateNumeric.Value;
					break;
				case 3:
					estFrequency = (long)LargeSavestateNumeric.Value;
					break;
			}

			long estTotalFrames = estFrames * estFrequency;
			double minutes = estTotalFrames / 60 / 60;

			AverageStoredStateSizeLabel.Text = FormatKB(avgStateSize);
			ApproxFramesLabel.Text = $"{estFrames:n0} frames";
			EstTimeLabel.Text = $"{minutes:n} minutes";
		}

		private void BufferSizeUpDown_ValueChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void UseDeltaCompression_CheckedChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void SmallSavestateNumeric_ValueChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void MediumSavestateNumeric_ValueChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void LargeSavestateNumeric_ValueChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void NudCompression_ValueChanged(object sender, EventArgs e)
		{
			trackBarCompression.Value = (int)((NumericUpDown)sender).Value;
		}

		private void TrackBarCompression_ValueChanged(object sender, EventArgs e)
		{
			// TODO - make a UserControl which is TrackBar and NUD combined
			nudCompression.Value = ((TrackBar)sender).Value;
		}

		private void BtnResetCompression_Click(object sender, EventArgs e)
		{
			nudCompression.Value = SaveStateConfig.DefaultCompressionLevelNormal;
		}
	}
}
