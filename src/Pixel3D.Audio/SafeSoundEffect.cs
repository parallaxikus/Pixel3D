﻿// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace Pixel3D.Audio
{
	/// <summary>
	///     Because XNA sucks, it will crash when attempting to create a sound if there is no audio hardware. We must
	///     wrap, because we need an object for serialization.
	/// </summary>
	public class SafeSoundEffect : IDisposable
	{
		/// <summary>The underlying sound effect (can be null)</summary>
		public SoundEffect inner;

	    static SafeSoundEffect()
	    {
	        SoundEffectVolume = 1.0f;
	    }

		public SafeSoundEffect()
		{
		}

		public SafeSoundEffect(SoundEffect inner)
		{
			this.inner = inner;
		}

		public void Dispose()
		{
            if(inner != null)
			   inner.Dispose();
		}

		/// <summary>How long is this sound effect in frames at a given pitch (NOTE: this value is not network-safe)</summary>
		public int DurationInFrames(float pitch)
		{
			if (inner == null)
				return 1; // <- oh well;

			// Making a reasonably safe assumption about how XNA pitch-bending works here:
			var seconds = inner.Duration.TotalSeconds;
			seconds = seconds / Math.Pow(2.0, pitch); // <- pitch bend changes duration

			return (int) Math.Ceiling(seconds * 60);
		}

		#region Wrapper

		public string Name
		{
		    get { return inner.Name; }
		    set { inner.Name = value; }
		}

		public bool Play()
		{
			// float volume, float pitch, float pan
			// soundEffect.Play(_sfxVolume, 0, 0);

			return inner != null && inner.Play(SoundEffectVolume, 0f, 0f);
		}

		public bool Play(float volume, float pitch, float pan)
		{
			return inner != null && inner.Play(volume * SoundEffectVolume, pitch, pan);
		}

		public bool Play(FadePitchPan fpp)
		{
			return inner != null && inner.Play(fpp.fade * SoundEffectVolume, fpp.pitch, fpp.pan);
		}

		/// <summary>Create an instance of the sound effect (can return null)</summary>
		public SafeSoundEffectInstance CreateInstance()
		{
			return new SafeSoundEffectInstance(inner.CreateInstance());
		}

		public static SafeSoundEffect FromStream(Stream stream)
		{
			if (!AudioDevice.Available)
				return new SafeSoundEffect();
			return new SafeSoundEffect(SoundEffect.FromStream(stream));
		}

		public static SafeSoundEffect FromFile(string path)
		{
			if (!AudioDevice.Available)
				return new SafeSoundEffect();

			using (var fs = File.OpenRead(path))
			{
				return new SafeSoundEffect(SoundEffect.FromStream(fs));
			}
		}

		#endregion

		#region Instance Pool (For SoundEffectManager only)

		// This is basically so that SoundEffectManager doesn't need to have a Dictionary lookup.
		// NOTE: Thread-safety is predicated on being inside the SoundEffectManager lock!!
		// NOTE: Network serialization cannot get at `instancePool` and cannot overwrite it (due to custom serializer, and it having no deserialize path)

		// TODO: Should probably expire old, unused instances
		private readonly List<SafeSoundEffectInstance> instancePool = new List<SafeSoundEffectInstance>();

		/// <summary>
		///     IMPORTANT: We assume you will fully set the Volume, Pitch and Pan properties. We assume you never set
		///     IsLooped!
		/// </summary>
		public SafeSoundEffectInstance SoundEffectManager_GetInstance()
		{
			if (instancePool.Count == 0)
			{
				return CreateInstance();
			}

			var instance = instancePool[instancePool.Count - 1];
			instancePool.RemoveAt(instancePool.Count - 1);
			return instance;
		}

		/// <summary>IMPORTANT: We assume you stopped the instance...</summary>
		public void SoundEffectManager_ReturnInstance(SafeSoundEffectInstance instance)
		{
			// NOTE: We cannot check if the sound is really stopped, because of the way threading works in the XNA sound library (ie: in a dumb way.)
			Debug.Assert(instance.IsLooped == false);
			instancePool.Add(instance);
		}

		#endregion

		#region Static Methods

		public static float MasterVolume
		{
		    get
		    {
                return AudioDevice.Available ? SoundEffect.MasterVolume : 0f;
		    }
			set
			{
				if (AudioDevice.Available && Math.Abs(SoundEffect.MasterVolume - value) > 0.0f) // <- avoid touching native sound engine if we'd just set the same value
					SoundEffect.MasterVolume = value;
			}
		}

		public static float SoundEffectVolume { get; set; }

		#endregion
	}
}