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
using DirectShowLib;
using Mediaportal.TV.Server.TVLibrary.Interfaces;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Diseqc;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;

namespace Mediaportal.TV.Server.Plugins.TunerExtension.DvbSky
{
  /// <summary>
  /// A class for handling conditional access and DiSEqC for DVBSky devices. Actually with the exception of
  /// the GUIDs, the DVBSky conditional access interface is identical to the NetUP conditional access
  /// interface, and their DiSEqC interface is identical to the Conexant interface. C# doesn't support
  /// multiple inheritence so we inherit from NetUP and use an internal Conexant instance.
  /// </summary>
  public class DvbSky : NetUp.NetUp
  {
    #region enums

    private enum BdaExtensionProperty
    {
      /// For sending and receiving DiSEqC messages.
      DiseqcMessage = 0,
      /// For receiving remote keypresses.
      RemoteCode,
      /// For passing blindscan parameters and commands.
      BlindscanCommand,
      /// For retrieving blindscan results.
      BlindscanData
    }

    #endregion

    #region constants

    private static readonly Guid DVBSKY_BDA_EXTENSION_GENERAL_PROPERTY_SET = new Guid(0x03cbdcb9, 0x36dc, 0x4072, 0xac, 0x42, 0x2f, 0x94, 0xf4, 0xec, 0xa0, 0x5e);
    private static readonly Guid DVBSKY_BDA_EXTENSION_CA_PROPERTY_SET = new Guid(0x4fdc5d3a, 0x1543, 0x479e, 0x9f, 0xc3, 0xb7, 0xdb, 0xa4, 0x73, 0xfb, 0x95);

    #endregion

    #region variables

    private bool _isDvbSky = false;

    private Conexant.Conexant _conexantInterface = null;

    #endregion

    /// <summary>
    /// Accessor for the property set GUID. This enables easy inheritence from the NetUP base class.
    /// </summary>
    /// <value>the GUID for the driver's custom property set</value>
    protected override Guid BdaExtensionPropertySet
    {
      get
      {
        return DVBSKY_BDA_EXTENSION_CA_PROPERTY_SET;
      }
    }

    #region ICustomDevice members

    /// <summary>
    /// The loading priority for this device type.
    /// </summary>
    public override byte Priority
    {
      get
      {
        return 75;
      }
    }

    /// <summary>
    /// A human-readable name for the device. This could be a manufacturer or reseller name, or even a model
    /// name/number.
    /// </summary>
    public override string Name
    {
      get
      {
        return "DVBSky";
      }
    }

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
      this.LogDebug("DVBSky: initialising device");

      if (_isDvbSky)
      {
        this.LogDebug("DVBSky: device is already initialised");
        return true;
      }

      this.LogDebug("DVBSky: checking base Conexant interface support");
      _conexantInterface = new Conexant.Conexant(DVBSKY_BDA_EXTENSION_GENERAL_PROPERTY_SET);
      if (!_conexantInterface.Initialise(tunerExternalIdentifier, tunerType, context))
      {
        this.LogDebug("DVBSky: base Conexant interface not supported");
        _conexantInterface.Dispose();
        _conexantInterface = null;
        return false;
      }
      this.LogDebug("DVBSky: base Conexant interface supported");
      _isDvbSky = true;

      this.LogDebug("DVBSky: checking base NetUP conditional access support");
      if (base.Initialise(tunerExternalIdentifier, tunerType, context))
      {
        this.LogDebug("DVBSky: conditional access interface supported");
      }
      else
      {
        this.LogDebug("DVBSky: conditional access interface not supported");
      }
      return true;
    }

    #endregion

    #region IDiseqcDevice members

    /// <summary>
    /// Control whether tone/data burst and 22 kHz legacy tone are used.
    /// </summary>
    /// <param name="toneBurstState">The tone/data burst state.</param>
    /// <param name="tone22kState">The 22 kHz legacy tone state.</param>
    /// <returns><c>true</c> if the tone state is set successfully, otherwise <c>false</c></returns>
    public override bool SetToneState(ToneBurst toneBurstState, Tone22k tone22kState)
    {
      this.LogDebug("DVBSky: set tone state, burst = {0}, 22 kHz = {1}", toneBurstState, tone22kState);

      if (!_isDvbSky || _conexantInterface == null)
      {
        this.LogDebug("DVBSky: device not initialised or interface not supported");
        return false;
      }

      return _conexantInterface.SetToneState(toneBurstState, tone22kState);
    }

    /// <summary>
    /// Send an arbitrary DiSEqC command.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <returns><c>true</c> if the command is sent successfully, otherwise <c>false</c></returns>
    public override bool SendCommand(byte[] command)
    {
      this.LogDebug("DVBSky: send DiSEqC command");

      if (!_isDvbSky || _conexantInterface == null)
      {
        this.LogDebug("DVBSky: device not initialised or interface not supported");
        return false;
      }

      return _conexantInterface.SendCommand(command);
    }

    /// <summary>
    /// Retrieve the response to a previously sent DiSEqC command (or alternatively, check for a command
    /// intended for this tuner).
    /// </summary>
    /// <param name="response">The response (or command).</param>
    /// <returns><c>true</c> if the response is read successfully, otherwise <c>false</c></returns>
    public override bool ReadResponse(out byte[] response)
    {
      this.LogDebug("DVBSky: read DiSEqC response");
      response = null;

      if (!_isDvbSky || _conexantInterface == null)
      {
        this.LogDebug("DVBSky: device not initialised or interface not supported");
        return false;
      }

      return _conexantInterface.ReadResponse(out response);
    }

    #endregion

    #region IDisposable member

    /// <summary>
    /// Release and dispose all resources.
    /// </summary>
    public override void Dispose()
    {
      if (_conexantInterface != null)
      {
        _conexantInterface.Dispose();
        _conexantInterface = null;
      }
      base.Dispose();
      _isDvbSky = false;
    }

    #endregion
  }
}
