﻿#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Runtime.InteropServices;
using DirectShowLib;
using Mediaportal.TV.Server.TVLibrary.Interfaces;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Diseqc;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using Mediaportal.TV.Server.TVLibrary.Interfaces.TunerExtension;

namespace Mediaportal.TV.Server.Plugins.TunerExtension.Compro
{
  public class Compro : BaseCustomDevice, IPowerDevice, IDiseqcDevice
  {
    #region enums

    private enum BdaExtensionDiseqcProperty
    {
      DiseqcBasic = 0,
      DiseqcRaw,
      TonePower
    }

    private enum BdaExtensionProperty
    {
      MacAddress = 0
    }

    private enum Compro22k : byte
    {
      Off = 0,
      On
    }

    private enum ComproLnbPower : byte
    {
      Off = 0x02,
      On = 0x03
    }

    private enum ComproSwitchType
    {
      None = 0,
      Single,
      Tone,               // 22 kHz 2 port
      Mini,               // tone burst 2 port
      Diseqc1_0,          // DiSEqC 1.0 4 port (committed)
      Diseqc1_2           // DiSEqC 1.2 16 port (uncommitted)
    }

    #endregion

    #region constants

    private static readonly Guid BDA_EXTENSION_DISEQC_PROPERTY_SET = new Guid(0x0c12bf87, 0x5bc0, 0x4dda, 0x9d, 0x07, 0x21, 0xe5, 0xc2, 0xf3, 0xb9, 0xae);
    private static readonly Guid BDA_EXTENSION_PROPERTY_SET = new Guid(0xa1aa3f96, 0x2ea, 0x4ccb, 0xa7, 0x14, 0x0, 0xbc, 0xd3, 0x98, 0xad, 0xb4);

    private const int KS_PROPERTY_SIZE = 24;
    private const int MAX_DISEQC_MESSAGE_LENGTH = 8;
    private const int MAC_ADDRESS_LENGTH = 6;
    private const int GENERAL_BUFFER_SIZE = KS_PROPERTY_SIZE + 4;

    #endregion

    #region variables

    private bool _isCompro = false;
    private IntPtr _generalBuffer = IntPtr.Zero;
    private IntPtr _commandBuffer = IntPtr.Zero;
    private IKsPropertySet _propertySet = null;

    #endregion

    /// <summary>
    /// Attempt to read the device information from the tuner.
    /// </summary>
    private void ReadDeviceInfo()
    {
      this.LogDebug("Compro: read device information");

      // MAC address.
      this.LogDebug("Compro: reading MAC address");
      for (int i = 0; i < MAC_ADDRESS_LENGTH; i++)
      {
        Marshal.WriteByte(_generalBuffer, i, 0);
      }
      int returnedByteCount;
      int hr = _propertySet.Get(BDA_EXTENSION_PROPERTY_SET, (int)BdaExtensionProperty.MacAddress,
        _generalBuffer, MAC_ADDRESS_LENGTH,
        _generalBuffer, MAC_ADDRESS_LENGTH,
        out returnedByteCount
      );
      if (hr != (int)HResult.Severity.Success || returnedByteCount != MAC_ADDRESS_LENGTH)
      {
        this.LogDebug("Compro: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      }
      else
      {
        string address = string.Empty;
        for (int i = 0; i < returnedByteCount; i++)
        {
          address += string.Format("{0:x2}-", Marshal.ReadByte(_generalBuffer, i));
        }
        this.LogDebug("  MAC address = {0}", address.Substring(0, (returnedByteCount * 3) - 1));
      }
    }

    #region ICustomDevice members

    /// <summary>
    /// Attempt to initialise the device-specific interfaces supported by the class. If initialisation fails,
    /// the ICustomDevice instance should be disposed immediately.
    /// </summary>
    /// <param name="tunerExternalIdentifier">The external identifier for the tuner.</param>
    /// <param name="tunerType">The tuner type (eg. DVB-S, DVB-T... etc.).</param>
    /// <param name="context">Context required to initialise the interface.</param>
    /// <returns><c>true</c> if the interfaces are successfully initialised, otherwise <c>false</c></returns>
    public override bool Initialise(string tunerExternalIdentifier, CardType tunerType, object context)
    {
      this.LogDebug("Compro: initialising device");

      if (context == null)
      {
        this.LogDebug("Compro: context is null");
        return false;
      }
      if (_isCompro)
      {
        this.LogDebug("Compro: device is already initialised");
        return true;
      }

      _propertySet = context as IKsPropertySet;
      if (_propertySet == null)
      {
        this.LogDebug("Compro: context is not a property set");
        return false;
      }

      KSPropertySupport support;
      int hr = _propertySet.QuerySupported(BDA_EXTENSION_DISEQC_PROPERTY_SET, (int)BdaExtensionDiseqcProperty.DiseqcRaw, out support);
      if (hr != (int)HResult.Severity.Success || (support & KSPropertySupport.Get) == 0)
      {
        this.LogDebug("Compro: device does not support the Compro property set, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
        return false;
      }

      this.LogDebug("Compro: supported device detected");
      _isCompro = true;
      _generalBuffer = Marshal.AllocCoTaskMem(GENERAL_BUFFER_SIZE);
      _commandBuffer = Marshal.AllocCoTaskMem(MAX_DISEQC_MESSAGE_LENGTH);
      ReadDeviceInfo();
      return true;
    }

    #endregion

    #region IPowerDevice member

    /// <summary>
    /// Set the tuner power state.
    /// </summary>
    /// <param name="state">The power state to apply.</param>
    /// <returns><c>true</c> if the power state is set successfully, otherwise <c>false</c></returns>
    public bool SetPowerState(PowerState state)
    {
      this.LogDebug("Compro: set power state, state = {0}", state);

      if (!_isCompro || _propertySet == null)
      {
        this.LogDebug("Compro: device not initialised or interface not supported");
        return false;
      }

      KSPropertySupport support;
      int hr = _propertySet.QuerySupported(BDA_EXTENSION_DISEQC_PROPERTY_SET, (int)BdaExtensionDiseqcProperty.TonePower, out support);
      if (hr != (int)HResult.Severity.Success || (support & KSPropertySupport.Set) == 0)
      {
        this.LogDebug("Compro: property not supported, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
        return false;
      }

      if (state == PowerState.On)
      {
        Marshal.WriteByte(_commandBuffer, 0, (byte)ComproLnbPower.On);
      }
      else
      {
        Marshal.WriteByte(_commandBuffer, 0, (byte)ComproLnbPower.Off);
      }

      hr = _propertySet.Set(BDA_EXTENSION_DISEQC_PROPERTY_SET, (int)BdaExtensionDiseqcProperty.TonePower,
        IntPtr.Zero, 0,
        _commandBuffer, sizeof(Byte)
      );
      if (hr == (int)HResult.Severity.Success)
      {
        this.LogDebug("Compro: result = success");
        return true;
      }

      this.LogDebug("Compro: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      return false;
    }

    #endregion

    #region IDiseqcDevice members

    /// <summary>
    /// Control whether tone/data burst and 22 kHz legacy tone are used.
    /// </summary>
    /// <param name="toneBurstState">The tone/data burst state.</param>
    /// <param name="tone22k">The 22 kHz legacy tone state.</param>
    /// <returns><c>true</c> if the tone state is set successfully, otherwise <c>false</c></returns>
    public bool SetToneState(ToneBurst toneBurstState, Tone22k tone22kState)
    {
      this.LogDebug("Compro: set tone state, burst = {0}, 22 kHz = {1}", toneBurstState, tone22kState);

      if (!_isCompro || _propertySet == null)
      {
        this.LogDebug("Compro: device not initialised or interface not supported");
        return false;
      }

      bool success = true;
      KSPropertySupport support;
      int hr;

      if (toneBurstState != ToneBurst.None)
      {
        hr = _propertySet.QuerySupported(BDA_EXTENSION_DISEQC_PROPERTY_SET, (int)BdaExtensionDiseqcProperty.DiseqcBasic,
                                    out support);
        if (hr != (int)HResult.Severity.Success || (support & KSPropertySupport.Set) == 0)
        {
          this.LogDebug("Compro: DiSEqC basic property not supported, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
        }
        else
        {
          Marshal.WriteInt32(_generalBuffer, 0, (int)ComproSwitchType.Mini);
          if (toneBurstState == ToneBurst.ToneBurst)
          {
            Marshal.WriteInt32(_commandBuffer, 0, 0);
          }
          else if (toneBurstState == ToneBurst.DataBurst)
          {
            Marshal.WriteInt32(_commandBuffer, 0, 1);
          }
          hr = _propertySet.Set(BDA_EXTENSION_DISEQC_PROPERTY_SET, (int)BdaExtensionDiseqcProperty.DiseqcBasic,
            _generalBuffer, sizeof(int),
            _commandBuffer, sizeof(int)
          );
          if (hr != (int)HResult.Severity.Success)
          {
            this.LogDebug("Compro: failed to set tone state, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
            success = false;
          }
        }
      }

      hr = _propertySet.QuerySupported(BDA_EXTENSION_DISEQC_PROPERTY_SET, (int)BdaExtensionDiseqcProperty.TonePower,
                                  out support);
      if (hr != (int)HResult.Severity.Success || (support & KSPropertySupport.Set) == 0)
      {
        this.LogDebug("Compro: tone/power property not supported, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      }
      else
      {
        if (tone22kState == Tone22k.On)
        {
          Marshal.WriteByte(_commandBuffer, 0, (byte)Compro22k.On);
        }
        else
        {
          Marshal.WriteByte(_commandBuffer, 0, (byte)Compro22k.Off);
        }
        hr = _propertySet.Set(BDA_EXTENSION_DISEQC_PROPERTY_SET, (int)BdaExtensionDiseqcProperty.TonePower,
          IntPtr.Zero, 0,
          _commandBuffer, sizeof(Byte)
        );
        if (hr != (int)HResult.Severity.Success)
        {
          this.LogDebug("Compro: failed to set 22 kHz state, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
          success = false;
        }
      }

      if (success)
      {
        this.LogDebug("Compro: result = success");
      }
      return success;
    }

    /// <summary>
    /// Send an arbitrary DiSEqC command.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <returns><c>true</c> if the command is sent successfully, otherwise <c>false</c></returns>
    public bool SendCommand(byte[] command)
    {
      this.LogDebug("Compro: send DiSEqC command");

      if (!_isCompro || _propertySet == null)
      {
        this.LogDebug("Compro: device not initialised or interface not supported");
        return false;
      }
      if (command == null || command.Length == 0)
      {
        this.LogDebug("Compro: command not supplied");
        return true;
      }
      if (command.Length > MAX_DISEQC_MESSAGE_LENGTH)
      {
        this.LogDebug("Compro: command too long, length = {0}", command.Length);
        return false;
      }

      for (int i = 0; i < GENERAL_BUFFER_SIZE; i++)
      {
        Marshal.WriteByte(_generalBuffer, i, 0);
      }
      Marshal.WriteInt32(_generalBuffer, 0, command.Length);

      Marshal.Copy(command, 0, _commandBuffer, command.Length);

      int hr = _propertySet.Set(BDA_EXTENSION_DISEQC_PROPERTY_SET, (int)BdaExtensionDiseqcProperty.DiseqcRaw,
        _generalBuffer, GENERAL_BUFFER_SIZE,
        _commandBuffer, MAX_DISEQC_MESSAGE_LENGTH
      );
      if (hr == (int)HResult.Severity.Success)
      {
        this.LogDebug("Compro: result = success");
        return true;
      }

      this.LogDebug("Compro: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      return false;
    }

    /// <summary>
    /// Retrieve the response to a previously sent DiSEqC command (or alternatively, check for a command
    /// intended for this tuner).
    /// </summary>
    /// <param name="response">The response (or command).</param>
    /// <returns><c>true</c> if the response is read successfully, otherwise <c>false</c></returns>
    public bool ReadResponse(out byte[] response)
    {
      // Not implemented.
      response = null;
      return false;
    }

    #endregion

    #region IDisposable member

    /// <summary>
    /// Release and dispose all resources.
    /// </summary>
    public override void Dispose()
    {
      if (_generalBuffer != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_generalBuffer);
        _generalBuffer = IntPtr.Zero;
      }
      if (_commandBuffer != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_commandBuffer);
        _commandBuffer = IntPtr.Zero;
      }
      _isCompro = false;
    }

    #endregion
  }
}
