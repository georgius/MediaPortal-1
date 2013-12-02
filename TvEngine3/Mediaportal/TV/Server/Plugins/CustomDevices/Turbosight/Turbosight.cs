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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using DirectShowLib;
using DirectShowLib.BDA;
using MediaPortal.Common.Utils;
using Mediaportal.TV.Server.TVLibrary.Interfaces;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Diseqc;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Implementations.Channels;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Interfaces;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using Mediaportal.TV.Server.TVLibrary.Interfaces.TunerExtension;

namespace Mediaportal.TV.Server.Plugins.TunerExtension.Turbosight
{
  /// <summary>
  /// A class for handling conditional access and DiSEqC for Turbosight tuners. Note that some Turbosight drivers
  /// seem to still support the original Conexant, NXP and Cyprus interfaces/structures. However, it is
  /// simpler and definitely more future-proof to stick with the information in the published SDK.
  /// </summary>
  public class Turbosight : BaseCustomDevice, IPowerDevice, IConditionalAccessProvider, ICiMenuActions, IDiseqcDevice
  {
    #region enums

    // PCIe/PCI only.
    private enum BdaExtensionProperty
    {
      Reserved = 0,
      NbcParams = 10,     // Property for setting DVB-S2 parameters that could not initially be set through BDA interfaces.
      BlindScan = 11,     // Property for accessing and controlling the hardware blind scan capabilities.
      CiAccess = 18,      // Property for interacting with the CI slot.
      TbsAccess = 21      // TBS property for enabling control of the common properties in the BdaExtensionCommand enum.
    }

    // USB (QBOX) only.
    private enum UsbBdaExtensionProperty
    {
      Reserved = 0,
      Ir = 1,             // Property for retrieving IR codes from the device's IR receiver.
      CiAccess = 8,       // Property for interacting with the CI slot.
      BlindScan = 9,      // Property for accessing and controlling the hardware blind scan capabilities.
      TbsAccess = 18      // TBS property for enabling control of the common properties in the TbsAccessMode enum.
    }

    // Common properties that can be controlled on all TBS products.
    private enum TbsAccessMode : uint
    {
      LnbPower = 0,       // Control the LNB power supply.
      Diseqc,             // Send and receive DiSEqC messages.
      Tone                // Control the 22 kHz oscillator state.
    }

    private enum TbsLnbPower : uint
    {
      Off = 0,
      High,               // 18 V - linear horizontal, circular left.
      Low,                // 13 V - linear vertical, circular right.
      On                  // Power on using the previous voltage.
    }

    private enum TbsTone : uint
    {
      Off = 0,
      On,                 // Continuous tone on.
      BurstUnmodulated,   // Simple DiSEqC port A (tone burst).
      BurstModulated      // Simple DiSEqC port B (data burst).
    }

    private enum TbsPilot : uint
    {
      Off = 0,
      On,
      Unknown               // (Not used...)
    }

    private enum TbsRollOff : uint
    {
      Undefined = 0xff,
      Twenty = 0,           // 0.2
      TwentyFive,           // 0.25
      ThirtyFive            // 0.35
    }

    private enum TbsDvbsStandard : uint
    {
      Auto = 0,
      Dvbs,
      Dvbs2
    }

    private enum TbsMmiMessageType : byte
    {
      Null = 0,
      ApplicationInfo = 0x01,     // PC <-->
      CaInfo = 0x02,              // PC <-->
      //CaPmt = 0x03,               // PC -->
      //CaPmtReply = 0x04,          // PC <--
      DateTimeEnquiry = 0x05,     // PC <--
      //DateTime = 0x06,            // PC -->
      Enquiry = 0x07,             // PC <--
      Answer = 0x08,              // PC -->
      EnterMenu = 0x09,           // PC -->
      Menu = 0x0a,                // PC <--
      MenuAnswer = 0x0b,          // PC -->
      List = 0x0c,                // PC <--
      GetMmi = 0x0d,              // PC <--
      CloseMmi = 0x0e,            // PC -->
      //DateTimeMode = 0x10,        // PC -->
      //SetDateTime = 0x12          // PC <--
    }

    #region IR remote

    // PCIe/PCI only.
    private enum TbsIrProperty
    {
      Codes = 0,
      ReceiverCommand
    }

    private enum TbsIrCode : byte
    {
      Recall = 0x80,
      Up1 = 0x81,
      Right1 = 0x82,
      Record = 0x83,
      Power = 0x84,
      Three = 0x85,
      Two = 0x86,
      One = 0x87,
      Down1 = 0x88,
      Six = 0x89,
      Five = 0x8a,
      Four = 0x8b,
      Left1 = 0x8c,
      Nine = 0x8d,
      Eight = 0x8e,
      Seven = 0x8f,
      Left2 = 0x90,
      Up2 = 0x91,
      Zero = 0x92,
      Right2 = 0x93,
      Mute = 0x94,
      Tab = 0x95,
      Down2 = 0x96,
      Epg = 0x97,
      Pause = 0x98,
      Ok = 0x99,
      Snapshot = 0x9a,
      Info = 0x9c,
      Play = 0x9b,
      FullScreen = 0x9d,
      Menu = 0x9e,
      Exit = 0x9f
    }

    // PCIe/PCI only.
    private enum TbsIrReceiverCommand : byte
    {
      Start = 1,
      Stop,
      Flush
    }

    #endregion

    #endregion

    #region structs

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TbsAccessParams
    {
      public TbsAccessMode AccessMode;
      public TbsTone Tone;
      private uint Reserved1;
      public TbsLnbPower LnbPower;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DISEQC_MESSAGE_LENGTH)]
      public byte[] DiseqcTransmitMessage;
      public uint DiseqcTransmitMessageLength;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DISEQC_MESSAGE_LENGTH)]
      public byte[] DiseqcReceiveMessage;
      public uint DiseqcReceiveMessageLength;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
      private byte[] Reserved2;
    }

    // Used to improve tuning speeds for older Conexant-based tuners.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct NbcTuningParams
    {
      public TbsRollOff RollOff;
      public TbsPilot Pilot;
      public TbsDvbsStandard DvbsStandard;
      public BinaryConvolutionCodeRate InnerFecRate;
      public ModulationType ModulationType;
    }

    // PCIe/PCI only.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct IrCommand
    {
      public uint Address;
      public uint Command;
    }

    // USB (QBOX) only.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct UsbIrCommand
    {
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
      private byte[] Reserved1;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
      public byte[] Codes;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 244)]
      private byte[] Reserved2;
    }

    // MP internal message holder - purely for convenience.
    private struct MmiMessage
    {
      public TbsMmiMessageType Type;
      public int Length;
      public byte[] Message;

      public MmiMessage(TbsMmiMessageType type, int length)
      {
        Type = type;
        Length = length;
        Message = new byte[length];
      }

      public MmiMessage(TbsMmiMessageType type)
      {
        Type = type;
        Length = 0;
        Message = null;
      }
    }

    #endregion

    #region delegates

    /// <summary>
    /// Open the conditional access interface for a specific Turbosight device.
    /// </summary>
    /// <param name="tunerFilter">The tuner filter.</param>
    /// <param name="deviceName">The corresponding DsDevice name.</param>
    /// <param name="deviceIndex">A unique index for the device. This index enables CI support for multiple instances of a product.</param>
    /// <returns>a handle that the DLL can use to identify this device for future function calls</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate IntPtr On_Start_CI(IBaseFilter tunerFilter, [MarshalAs(UnmanagedType.LPWStr)] string deviceName, int deviceIndex);

    /// <summary>
    /// Check whether a CAM is present in the CI slot associated with a specific Turbosight device.
    /// </summary>
    /// <param name="handle">The handle allocated to this device.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool Camavailable(IntPtr handle);

    /// <summary>
    /// Exchange MMI messages with the CAM.
    /// </summary>
    /// <param name="handle">The handle allocated to this device.</param>
    /// <param name="command">The MMI command.</param>
    /// <param name="response">The MMI response.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TBS_ci_MMI_Process(IntPtr handle, IntPtr command, IntPtr response);

    /// <summary>
    /// Send PMT to the CAM.
    /// </summary>
    /// <param name="handle">The handle allocated to this device.</param>
    /// <param name="pmt">The PMT command.</param>
    /// <param name="pmtLength">The length of the PMT.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TBS_ci_SendPmt(IntPtr handle, IntPtr pmt, ushort pmtLength);

    /// <summary>
    /// Close the conditional access interface for a specific Turbosight device.
    /// </summary>
    /// <param name="handle">The handle allocated to this device.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void On_Exit_CI(IntPtr handle);

    #endregion

    #region constants

    private static readonly Guid BDA_EXTENSION_PROPERTY_SET = new Guid(0xfaa8f3e5, 0x31d4, 0x4e41, 0x88, 0xef, 0xd9, 0xeb, 0x71, 0x6f, 0x6e, 0xc9);
    private static readonly Guid USB_BDA_EXTENSION_PROPERTY_SET = new Guid(0xc6efe5eb, 0x855a, 0x4f1b, 0xb7, 0xaa, 0x87, 0xb5, 0xe1, 0xdc, 0x41, 0x13);
    private static readonly Guid IR_PROPERTY_SET = new Guid(0xb51c4994, 0x0054, 0x4749, 0x82, 0x43, 0x02, 0x9a, 0x66, 0x86, 0x36, 0x36);

    private static readonly int TBS_ACCESS_PARAMS_SIZE = Marshal.SizeOf(typeof(TbsAccessParams));   // 536
    private static readonly int NBC_TUNING_PARAMS_SIZE = Marshal.SizeOf(typeof(NbcTuningParams));   // 20
    private const int MAX_DISEQC_MESSAGE_LENGTH = 128;

    private const int MMI_MESSAGE_BUFFER_SIZE = 512;
    private const int MMI_RESPONSE_BUFFER_SIZE = 2048;

    private static readonly int GENERAL_BUFFER_SIZE = Math.Max(TBS_ACCESS_PARAMS_SIZE, NBC_TUNING_PARAMS_SIZE);
    private const int MMI_HANDLER_THREAD_SLEEP_TIME = 2000;   // unit = ms

    #endregion

    #region variables

    // This variable tracks the number of open API instances which corresponds with used DLL indices.
    private static int _apiCount = 0;

    // Conditional access API instance variables.
    private int _apiIndex = 0;
    private bool _dllLoaded = false;
    private IntPtr _libHandle = IntPtr.Zero;

    // Delegate instances for each API DLL function.
    private On_Start_CI _onStartCi = null;
    private Camavailable _camAvailable = null;
    private TBS_ci_MMI_Process _mmiProcess = null;
    private TBS_ci_SendPmt _sendPmt = null;
    private On_Exit_CI _onExitCi = null;

    // Buffers for use in conditional access related functions.
    private IntPtr _mmiMessageBuffer = IntPtr.Zero;
    private IntPtr _mmiResponseBuffer = IntPtr.Zero;
    private IntPtr _pmtBuffer = IntPtr.Zero;

    /// A buffer for general use in synchronised methods.
    private IntPtr _generalBuffer = IntPtr.Zero;

    private IntPtr _ciHandle = IntPtr.Zero;

    private IBaseFilter _tunerFilter = null;
    private string _tunerFilterName = null;

    private Guid _propertySetGuid = Guid.Empty;
    private IKsPropertySet _propertySet = null;
    private int _tbsAccessProperty = 0;

    private bool _isTurbosight = false;
    private bool _isUsb = false;
    private bool _isCiSlotPresent = false;
    private bool _isCamPresent = false;

    private volatile bool _stopMmiHandlerThread = false;
    private Thread _mmiHandlerThread = null;
    private ICiMenuCallbacks _ciMenuCallbacks = null;

    // This is a first-in-first-out queue of messages that are ready to be passed to the CAM.
    private List<MmiMessage> _mmiMessageQueue = null;

    #endregion

    /// <summary>
    /// Load a conditional access API instance. This involves obtaining delegate instances for
    /// each of the member functions.
    /// </summary>
    /// <returns><c>true</c> if the instance is successfully loaded, otherwise <c>false</c></returns>
    private bool LoadNewCaApiInstance()
    {
      // Load a new DLL. DLLs should not be reused to avoid issues when resetting interfaces and
      // enable support for multiple tuners with CI slots.
      _apiCount++;
      _apiIndex = _apiCount;
      this.LogDebug("Turbosight: loading API, API index = {0}", _apiIndex);
      string resourcesFolder = PathManager.BuildAssemblyRelativePath("Resources"); // It's called already inside plugins\CustomDevices !
      string sourceFilename = Path.Combine(resourcesFolder, "tbsCIapi.dll");
      string targetFilename = Path.Combine(resourcesFolder, "tbsCIapi" + _apiIndex + ".dll");
      if (!File.Exists(targetFilename))
      {
        try
        {
          File.Copy(sourceFilename, targetFilename);
        }
        catch (Exception ex)
        {
          this.LogError(ex, "Turbosight: failed to copy tbsCIapi.dll");
          return false;
        }
      }
      _libHandle = NativeMethods.LoadLibrary(targetFilename);
      if (_libHandle == IntPtr.Zero)
      {
        this.LogDebug("Turbosight: failed to load the DLL");
        return false;
      }

      try
      {
        IntPtr function = NativeMethods.GetProcAddress(_libHandle, "On_Start_CI");
        if (function == IntPtr.Zero)
        {
          this.LogDebug("Turbosight: failed to locate the On_Start_CI function");
          return false;
        }
        try
        {
          _onStartCi = (On_Start_CI)Marshal.GetDelegateForFunctionPointer(function, typeof(On_Start_CI));
        }
        catch (Exception ex)
        {
          this.LogError(ex, "Turbosight: failed to load the On_Start_CI function");
          return false;
        }

        function = NativeMethods.GetProcAddress(_libHandle, "Camavailable");
        if (function == IntPtr.Zero)
        {
          this.LogDebug("Turbosight: failed to locate the Camavailable function");
          return false;
        }
        try
        {
          _camAvailable = (Camavailable)Marshal.GetDelegateForFunctionPointer(function, typeof(Camavailable));
        }
        catch (Exception ex)
        {
          this.LogError(ex, "Turbosight: failed to load the Camavailable function");
          return false;
        }

        function = NativeMethods.GetProcAddress(_libHandle, "TBS_ci_MMI_Process");
        if (function == IntPtr.Zero)
        {
          this.LogDebug("Turbosight: failed to locate the TBS_ci_MMI_Process function");
          return false;
        }
        try
        {
          _mmiProcess = (TBS_ci_MMI_Process)Marshal.GetDelegateForFunctionPointer(function, typeof(TBS_ci_MMI_Process));
        }
        catch (Exception ex)
        {
          this.LogError(ex, "Turbosight: failed to load the TBS_ci_MMI_Process function");
          return false;
        }

        function = NativeMethods.GetProcAddress(_libHandle, "TBS_ci_SendPmt");
        if (function == IntPtr.Zero)
        {
          this.LogDebug("Turbosight: failed to locate the TBS_ci_SendPmt function");
          return false;
        }
        try
        {
          _sendPmt = (TBS_ci_SendPmt)Marshal.GetDelegateForFunctionPointer(function, typeof(TBS_ci_SendPmt));
        }
        catch (Exception ex)
        {
          this.LogError(ex, "Turbosight: failed to load the TBS_ci_SendPmt function");
          return false;
        }

        function = NativeMethods.GetProcAddress(_libHandle, "On_Exit_CI");
        if (function == IntPtr.Zero)
        {
          this.LogDebug("Turbosight: failed to locate the On_Exit_CI function");
          return false;
        }
        try
        {
          _onExitCi = (On_Exit_CI)Marshal.GetDelegateForFunctionPointer(function, typeof(On_Exit_CI));
        }
        catch (Exception ex)
        {
          this.LogError(ex, "Turbosight: failed to load the On_Exit_CI function");
          return false;
        }

        _dllLoaded = true;
        return true;
      }
      finally
      {
        if (!_dllLoaded)
        {
          NativeMethods.FreeLibrary(_libHandle);
          _libHandle = IntPtr.Zero;
        }
      }
    }

    #region MMI handler thread

    /// <summary>
    /// Start a thread that will handle interaction with the CAM.
    /// </summary>
    private void StartMmiHandlerThread()
    {
      // Don't start a thread if there is no purpose for it.
      if (!_isTurbosight || !_isCiSlotPresent || _ciHandle == IntPtr.Zero)
      {
        return;
      }

      // Check if an existing thread is still alive. It will be terminated in case of errors, i.e. when CI callback failed.
      if (_mmiHandlerThread != null && !_mmiHandlerThread.IsAlive)
      {
        this.LogDebug("Turbosight: aborting old MMI handler thread");
        _mmiHandlerThread.Abort();
        _mmiHandlerThread = null;
      }
      if (_mmiHandlerThread == null)
      {
        this.LogDebug("Turbosight: starting new MMI handler thread");
        // Clear the message queue and buffers in preparation for [first] use.
        _mmiMessageQueue = new List<MmiMessage>();
        for (int i = 0; i < MMI_MESSAGE_BUFFER_SIZE; i++)
        {
          Marshal.WriteByte(_mmiMessageBuffer, i, 0);
        }
        for (int i = 0; i < MMI_RESPONSE_BUFFER_SIZE; i++)
        {
          Marshal.WriteByte(_mmiResponseBuffer, i, 0);
        }
        _stopMmiHandlerThread = false;
        _mmiHandlerThread = new Thread(new ThreadStart(MmiHandler));
        _mmiHandlerThread.Name = "Turbosight MMI handler";
        _mmiHandlerThread.IsBackground = true;
        _mmiHandlerThread.Priority = ThreadPriority.Lowest;
        _mmiHandlerThread.Start();
      }
    }

    /// <summary>
    /// Thread function for handling message passing to and from the CAM.
    /// </summary>
    private void MmiHandler()
    {
      this.LogDebug("Turbosight: MMI handler thread start polling");
      TbsMmiMessageType message = TbsMmiMessageType.Null;
      ushort sendCount = 0;
      try
      {
        while (!_stopMmiHandlerThread)
        {
          // Check for CAM state changes.
          bool newState;
          lock (this)
          {
            newState = _camAvailable(_ciHandle);
          }
          if (newState != _isCamPresent)
          {
            _isCamPresent = newState;
            this.LogDebug("Turbosight: CAM state change, CAM present = {0}", _isCamPresent);
            // If a CAM has just been inserted then clear the message queue - we consider
            // any old messages as invalid now.
            if (_isCamPresent)
            {
              lock (this)
              {
                _mmiMessageQueue = new List<MmiMessage>();
              }
              message = TbsMmiMessageType.Null;
            }
          }

          // If there is no CAM then we can't send or receive messages.
          if (!_isCamPresent)
          {
            Thread.Sleep(MMI_HANDLER_THREAD_SLEEP_TIME);
            continue;
          }

          // Are we still trying to get a response?
          if (message == TbsMmiMessageType.Null)
          {
            // No -> do we have a message to send?
            lock (this)
            {
              // Yes -> load it into the message buffer.
              if (_mmiMessageQueue.Count > 0)
              {
                message = _mmiMessageQueue[0].Type;
                this.LogDebug("Turbosight: sending message {0}", message);
                Marshal.WriteByte(_mmiMessageBuffer, 0, (byte)message);
                for (ushort i = 0; i < _mmiMessageQueue[0].Length; i++)
                {
                  Marshal.WriteByte(_mmiMessageBuffer, i + 1, _mmiMessageQueue[0].Message[i]);
                }
                sendCount = 0;
              }
              // No -> poll for unrequested messages from the CAM.
              else
              {
                Marshal.WriteByte(_mmiMessageBuffer, 0, (byte)TbsMmiMessageType.GetMmi);
              }
            }
          }

          // Send/resend the message.
          lock (this)
          {
            _mmiProcess(_ciHandle, _mmiMessageBuffer, _mmiResponseBuffer);
          }

          // Do we expect a response to this message?
          if (message == TbsMmiMessageType.EnterMenu || message == TbsMmiMessageType.MenuAnswer || message == TbsMmiMessageType.Answer || message == TbsMmiMessageType.CloseMmi)
          {
            // No -> remove this message from the queue and move on.
            lock (this)
            {
              _mmiMessageQueue.RemoveAt(0);
            }
            message = TbsMmiMessageType.Null;
            if (_mmiMessageQueue.Count == 0)
            {
              this.LogDebug("Turbosight: resuming polling...");
            }
            continue;
          }

          // Yes, we expect a response -> check for a response.
          TbsMmiMessageType response = TbsMmiMessageType.Null;
          response = (TbsMmiMessageType)Marshal.ReadByte(_mmiResponseBuffer, 4);
          if (response == TbsMmiMessageType.Null)
          {
            // Responses don't always arrive quickly so give the CAM time to respond if
            // the response isn't ready yet.
            Thread.Sleep(MMI_HANDLER_THREAD_SLEEP_TIME);

            // If we are waiting for a response to a message that we sent
            // directly and we haven't received a response after 10 requests
            // then give up and move on.
            if (message != TbsMmiMessageType.Null)
            {
              sendCount++;
              if (sendCount >= 10)
              {
                lock (this)
                {
                  _mmiMessageQueue.RemoveAt(0);
                }
                this.LogDebug("Turbosight: giving up on message {0}", message);
                message = TbsMmiMessageType.Null;
                if (_mmiMessageQueue.Count == 0)
                {
                  this.LogDebug("Turbosight: resuming polling...");
                }
              }
            }
            continue;
          }

          this.LogDebug("Turbosight: received MMI response {0} to message {1}", response, message);
          #region response handling

          // Get the response bytes.
          byte lsb = Marshal.ReadByte(_mmiResponseBuffer, 5);
          byte msb = Marshal.ReadByte(_mmiResponseBuffer, 6);
          int length = (256 * msb) + lsb;
          if (length > MMI_RESPONSE_BUFFER_SIZE - 7)
          {
            this.LogDebug("Turbosight: response too long, length = {0}", length);
            // We know we haven't got the complete response (DLL internal buffer overflow),
            // so wipe the message and response buffers and give up on this message.
            for (int i = 0; i < MMI_RESPONSE_BUFFER_SIZE; i++)
            {
              Marshal.WriteByte(_mmiResponseBuffer, i, 0);
            }
            // If we requested this response directly then remove the request
            // message from the queue.
            if (message != TbsMmiMessageType.Null)
            {
              lock (this)
              {
                _mmiMessageQueue.RemoveAt(0);
              }
              message = TbsMmiMessageType.Null;
              if (_mmiMessageQueue.Count == 0)
              {
                this.LogDebug("Turbosight: resuming polling...");
              }
            }
            continue;
          }

          this.LogDebug("Turbosight: response length = {0}", length);
          //DVB_MMI.DumpBinary(_mmiResponseBuffer, 0, length + 7);
          byte[] responseBytes = new byte[length];
          int j = 7;
          for (int i = 0; i < length; i++)
          {
            responseBytes[i] = Marshal.ReadByte(_mmiResponseBuffer, j++);
          }

          if (response == TbsMmiMessageType.ApplicationInfo)
          {
            HandleApplicationInformation(responseBytes, length);
          }
          else if (response == TbsMmiMessageType.CaInfo)
          {
            HandleCaInformation(responseBytes, length);
          }
          else if (response == TbsMmiMessageType.Menu || response == TbsMmiMessageType.List)
          {
            HandleMenu(responseBytes, length);
          }
          else if (response == TbsMmiMessageType.Enquiry)
          {
            HandleEnquiry(responseBytes, length);
          }
          else
          {
            this.LogDebug("Turbosight: unhandled response message {0}", response);
            DVB_MMI.DumpBinary(_mmiResponseBuffer, 0, length);
          }

          // A message has been handled and now we move on to handling the
          // next message or revert to polling for messages from the CAM.
          for (int i = 0; i < MMI_RESPONSE_BUFFER_SIZE; i++)
          {
            Marshal.WriteByte(_mmiResponseBuffer, i, 0);
          }
          // If we requested this response directly then remove the request
          // message from the queue.
          if (message != TbsMmiMessageType.Null)
          {
            lock (this)
            {
              _mmiMessageQueue.RemoveAt(0);
            }
            message = TbsMmiMessageType.Null;
            if (_mmiMessageQueue.Count == 0)
            {
              this.LogDebug("Turbosight: resuming polling...");
            }
          }
          #endregion
        }
      }
      catch (ThreadAbortException)
      {
      }
      catch (Exception ex)
      {
        this.LogError(ex, "Turbosight: exception in MMI handler thread");
        return;
      }
    }

    private void HandleApplicationInformation(byte[] content, int length)
    {
      this.LogDebug("Turbosight: application information");
      if (length < 5)
      {
        this.LogDebug("Turbosight: error, response too short");
        DVB_MMI.DumpBinary(content, 0, length);
        return;
      }
      MmiApplicationType type = (MmiApplicationType)content[0];
      string title = System.Text.Encoding.ASCII.GetString(content, 5, length - 5);
      this.LogDebug("  type         = {0}", type);
      this.LogDebug("  manufacturer = 0x{0:x}{1:x}", content[1], content[2]);
      this.LogDebug("  code         = 0x{0:x}{1:x}", content[3], content[4]);
      this.LogDebug("  menu title   = {0}", title);
    }

    private void HandleCaInformation(byte[] content, int length)
    {
      this.LogDebug("Turbosight: conditional access information");
      if (length == 0)
      {
        this.LogDebug("Turbosight: error, response too short");
        return;
      }
      int numCasIds = content[0];
      this.LogDebug("  # CAS IDs = {0}", numCasIds);
      int i = 1;
      int l = 1;
      while (l + 2 <= length)
      {
        this.LogDebug("  {0,-2}        = 0x{1:x2}{2:x2}", i, content[l + 1], content[l]);
        l += 2;
        i++;
      }
      if (length != ((numCasIds * 2) + 1))
      {
        this.LogDebug("Turbosight: error, unexpected numCasIds");
        DVB_MMI.DumpBinary(_mmiResponseBuffer, 0, length);
      }
    }

    private void HandleMenu(byte[] content, int length)
    {
      this.LogDebug("Turbosight: menu");
      if (length == 0)
      {
        this.LogDebug("Turbosight: error, response too short");
        return;
      }
      int numEntries = content[0];

      // Read all the entries into a list. Entries are NULL terminated.
      List<string> entries = new List<string>();
      string entry = string.Empty;
      int entryCount = 0;
      for (int i = 1; i < length; i++)
      {
        if (content[i] == 0)
        {
          entries.Add(entry);
          entryCount++;
          entry = string.Empty;
        }
        else
        {
          if (content[i] >= 32 && content[i] <= 126)
          {
            entry += (char)content[i];
          }
          else
          {
            entry += ' ';
          }
        }
      }
      entries.Add(entry);
      entryCount -= 2;
      if (entryCount < 0)
      {
        this.LogDebug("Turbosight: error, not enough menu entries");
        DVB_MMI.DumpBinary(content, 0, length);
        return;
      }

      if (_ciMenuCallbacks == null)
      {
        this.LogDebug("Turbosight: menu callbacks are not set");
      }

      this.LogDebug("  title     = {0}", entries[0]);
      this.LogDebug("  sub-title = {0}", entries[1]);
      this.LogDebug("  footer    = {0}", entries[2]);
      this.LogDebug("  # entries = {0}", numEntries);
      if (_ciMenuCallbacks != null)
      {
        try
        {
          _ciMenuCallbacks.OnCiMenu(entries[0], entries[1], entries[2], entryCount);
        }
        catch (Exception ex)
        {
          this.LogError(ex, "Turbosight: menu header callback exception");
        }
      }
      for (int i = 0; i < entryCount; i++)
      {
        this.LogDebug("  entry {0,-2}  = {1}", i + 1, entries[i + 3]);
        if (_ciMenuCallbacks != null)
        {
          try
          {
            _ciMenuCallbacks.OnCiMenuChoice(i, entries[i + 3]);
          }
          catch (Exception ex)
          {
            this.LogDebug("Turbosight: menu entry callback exception\r\n{0}", ex.ToString());
          }
        }
      }

      if (entryCount != numEntries)
      {
        this.LogDebug("Turbosight: error, numEntries != entryCount");
        //DVB_MMI.DumpBinary(_mmiResponseBuffer, 0, length);
      }
    }

    private void HandleEnquiry(byte[] content, int length)
    {
      this.LogDebug("Turbosight: enquiry");
      if (length < 3)
      {
        this.LogDebug("Turbosight: error, response too short");
        DVB_MMI.DumpBinary(content, 0, length);
        return;
      }
      bool blind = (content[0] != 0);
      uint answerLength = content[1];
      string text = System.Text.Encoding.ASCII.GetString(content, 2, length - 2);
      this.LogDebug("  text   = {0}", text);
      this.LogDebug("  length = {0}", answerLength);
      this.LogDebug("  blind  = {0}", blind);
      if (_ciMenuCallbacks != null)
      {
        try
        {
          _ciMenuCallbacks.OnCiRequest(blind, answerLength, text);
        }
        catch (Exception ex)
        {
          this.LogError(ex, "Turbosight: CAM request callback exception");
        }
      }
      else
      {
        this.LogDebug("Turbosight: menu callbacks are not set");
      }
    }

    #endregion

    #region ICustomDevice members

    /// <summary>
    /// The loading priority for this device type.
    /// </summary>
    public override byte Priority
    {
      get
      {
        return 70;
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
        if (string.IsNullOrEmpty(_tunerFilterName))
        {
          return base.Name;
        }
        return _tunerFilterName;
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
      this.LogDebug("Turbosight: initialising device");

      IBaseFilter tunerFilter = context as IBaseFilter;
      if (tunerFilter == null)
      {
        this.LogDebug("Turbosight: tuner filter is null");
        return false;
      }
      if (_isTurbosight)
      {
        this.LogDebug("Turbosight: device is already initialised");
        return true;
      }

      // Check the tuner filter name first. Other manufacturers that do not support these interfaces
      // use the same GUIDs which makes things a little tricky.
      FilterInfo tunerFilterInfo;
      int hr = tunerFilter.QueryFilterInfo(out tunerFilterInfo);
      _tunerFilterName = tunerFilterInfo.achName;
      Release.FilterInfo(ref tunerFilterInfo);
      if (hr != (int)HResult.Severity.Success || _tunerFilterName == null)
      {
        this.LogDebug("Turbosight: failed to get the tuner filter name, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
        return false;
      }
      if (!_tunerFilterName.StartsWith("TBS") && !_tunerFilterName.StartsWith("QBOX"))
      {
        this.LogDebug("Turbosight: tuner filter name does not match");
        return false;
      }
      this.LogDebug("Turbosight: checking device named \"{0}\"", _tunerFilterName);

      // Now check for the USB interface first as per TBS SDK recommendations.
      KSPropertySupport support;
      _propertySet = tunerFilter as IKsPropertySet;
      if (_propertySet != null)
      {
        hr = _propertySet.QuerySupported(USB_BDA_EXTENSION_PROPERTY_SET, (int)UsbBdaExtensionProperty.TbsAccess, out support);
        if (hr == (int)HResult.Severity.Success && support != 0)
        {
          // Okay, we've got a USB tuner here.
          this.LogDebug("Turbosight: supported device detected (USB interface)");
          _isTurbosight = true;
          _isUsb = true;
          _propertySetGuid = USB_BDA_EXTENSION_PROPERTY_SET;
          _tbsAccessProperty = (int)UsbBdaExtensionProperty.TbsAccess;
        }
      }

      // If the device doesn't support the USB interface then check for the PCIe/PCI interface.
      if (!_isTurbosight)
      {
        IPin pin = DsFindPin.ByDirection(tunerFilter, PinDirection.Input, 0);
        _propertySet = pin as IKsPropertySet;
        if (_propertySet != null)
        {
          hr = _propertySet.QuerySupported(BDA_EXTENSION_PROPERTY_SET, (int)BdaExtensionProperty.TbsAccess, out support);
          if (hr == (int)HResult.Severity.Success && support != 0)
          {
            // Okay, we've got a PCIe or PCI tuner here.
            this.LogDebug("Turbosight: supported device detected (PCIe/PCI interface)");
            _isTurbosight = true;
            _isUsb = false;
            _propertySetGuid = BDA_EXTENSION_PROPERTY_SET;
            _tbsAccessProperty = (int)BdaExtensionProperty.TbsAccess;
          }
        }
        if (!_isTurbosight)
        {
          Release.ComObject("Turbosight tuner filter input pin", ref pin);
        }
      }

      if (!_isTurbosight)
      {
        this.LogDebug("Turbosight: device does not support interfaces");
        return false;
      }

      _tunerFilter = tunerFilter;
      _generalBuffer = Marshal.AllocCoTaskMem(GENERAL_BUFFER_SIZE);
      return true;
    }

    #region device state change callbacks

    /// <summary>
    /// This callback is invoked before a tune request is assembled.
    /// </summary>
    /// <param name="tuner">The tuner instance that this device instance is associated with.</param>
    /// <param name="currentChannel">The channel that the tuner is currently tuned to..</param>
    /// <param name="channel">The channel that the tuner will been tuned to.</param>
    /// <param name="action">The action to take, if any.</param>
    public override void OnBeforeTune(ITVCard tuner, IChannel currentChannel, ref IChannel channel, out TunerAction action)
    {
      this.LogDebug("Turbosight: on before tune callback");
      action = TunerAction.Default;

      if (!_isTurbosight)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return;
      }

      // Parameters only need to be tweaked and set for DVB-S/2 tuning.
      DVBSChannel ch = channel as DVBSChannel;
      if (ch == null)
      {
        return;
      }

      NbcTuningParams command = new NbcTuningParams();
      // Default: tuning with "auto" is slower, so avoid it if possible.
      command.DvbsStandard = TbsDvbsStandard.Auto;

      // FEC rate
      command.InnerFecRate = ch.InnerFecRate;
      this.LogDebug("  inner FEC rate = {0}", command.InnerFecRate);

      // Modulation
      if (ch.ModulationType == ModulationType.ModNotSet)
      {
        ch.ModulationType = ModulationType.ModQpsk;
        command.DvbsStandard = TbsDvbsStandard.Dvbs;
      }
      else if (ch.ModulationType == ModulationType.ModQpsk)
      {
        ch.ModulationType = ModulationType.ModNbcQpsk;
        command.DvbsStandard = TbsDvbsStandard.Dvbs2;
      }
      else if (ch.ModulationType == ModulationType.Mod8Psk)
      {
        ch.ModulationType = ModulationType.ModNbc8Psk;
        command.DvbsStandard = TbsDvbsStandard.Dvbs2;
      }
      command.ModulationType = ch.ModulationType;
      this.LogDebug("  modulation     = {0}", ch.ModulationType);

      // Pilot
      if (ch.Pilot == Pilot.On)
      {
        command.Pilot = TbsPilot.On;
      }
      else
      {
        command.Pilot = TbsPilot.Off;
      }
      this.LogDebug("  pilot          = {0}", command.Pilot);

      // Roll-off
      if (ch.RollOff == RollOff.Twenty)
      {
        command.RollOff = TbsRollOff.Twenty;
      }
      else if (ch.RollOff == RollOff.TwentyFive)
      {
        command.RollOff = TbsRollOff.TwentyFive;
      }
      else if (ch.RollOff == RollOff.ThirtyFive)
      {
        command.RollOff = TbsRollOff.ThirtyFive;
      }
      else
      {
        command.RollOff = TbsRollOff.Undefined;
      }
      this.LogDebug("  roll-off       = {0}", command.RollOff);

      KSPropertySupport support;
      int hr = _propertySet.QuerySupported(BDA_EXTENSION_PROPERTY_SET, (int)BdaExtensionProperty.NbcParams, out support);
      if (hr != (int)HResult.Severity.Success || (support & KSPropertySupport.Set) == 0)
      {
        this.LogDebug("Turbosight: NBC tuning parameter property not supported, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
        return;
      }

      Marshal.StructureToPtr(command, _generalBuffer, true);
      //DVB_MMI.DumpBinary(_generalBuffer, 0, NBC_TUNING_PARAMS_SIZE);

      hr = _propertySet.Set(BDA_EXTENSION_PROPERTY_SET, (int)BdaExtensionProperty.NbcParams,
        _generalBuffer, NBC_TUNING_PARAMS_SIZE,
        _generalBuffer, NBC_TUNING_PARAMS_SIZE
      );
      if (hr == (int)HResult.Severity.Success)
      {
        this.LogDebug("Turbosight: result = success");
      }
      else
      {
        this.LogDebug("Turbosight: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      }
    }

    /// <summary>
    /// This callback is invoked after a tune request is submitted, when the device is started but before
    /// signal lock is checked.
    /// </summary>
    /// <param name="tuner">The tuner instance that this device instance is associated with.</param>
    /// <param name="currentChannel">The channel that the tuner is tuned to.</param>
    public override void OnStarted(ITVCard tuner, IChannel currentChannel)
    {
      // Ensure the MMI handler thread is always running when the graph is running.
      StartMmiHandlerThread();
    }

    #endregion

    #endregion

    #region IPowerDevice member

    /// <summary>
    /// Set the tuner power state.
    /// </summary>
    /// <param name="state">The power state to apply.</param>
    /// <returns><c>true</c> if the power state is set successfully, otherwise <c>false</c></returns>
    public bool SetPowerState(PowerState state)
    {
      this.LogDebug("Turbosight: set power state, state = {0}", state);

      if (!_isTurbosight)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }

      TbsAccessParams accessParams = new TbsAccessParams();
      accessParams.AccessMode = TbsAccessMode.LnbPower;
      if (state == PowerState.On)
      {
        accessParams.LnbPower = TbsLnbPower.On;
      }
      else
      {
        accessParams.LnbPower = TbsLnbPower.Off;
      }

      Marshal.StructureToPtr(accessParams, _generalBuffer, true);
      //DVB_MMI.DumpBinary(_generalBuffer, 0, TBS_ACCESS_PARAMS_SIZE);

      int hr = _propertySet.Set(_propertySetGuid, _tbsAccessProperty,
        _generalBuffer, TBS_ACCESS_PARAMS_SIZE,
        _generalBuffer, TBS_ACCESS_PARAMS_SIZE
      );
      if (hr == (int)HResult.Severity.Success)
      {
        this.LogDebug("Turbosight: result = success");
        return true;
      }

      this.LogDebug("Turbosight: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      return false;
    }

    #endregion

    #region IConditionalAccessProvider members

    /// <summary>
    /// Open the conditional access interface. For the interface to be opened successfully it is expected
    /// that any necessary hardware (such as a CI slot) is connected.
    /// </summary>
    /// <returns><c>true</c> if the interface is successfully opened, otherwise <c>false</c></returns>
    public bool OpenInterface()
    {
      this.LogDebug("Turbosight: open conditional access interface");

      if (!_isTurbosight)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }
      if (_dllLoaded || _ciHandle != IntPtr.Zero)
      {
        this.LogDebug("Turbosight: interface is already open");
        return false;
      }

      // Check whether a CI slot is present.
      _isCiSlotPresent = false;
      int ciAccessProperty = (int)BdaExtensionProperty.CiAccess;
      if (_isUsb)
      {
        ciAccessProperty = (int)UsbBdaExtensionProperty.CiAccess;
      }
      KSPropertySupport support;
      int hr = _propertySet.QuerySupported(_propertySetGuid, ciAccessProperty, out support);
      if (hr != (int)HResult.Severity.Success || support == 0)
      {
        this.LogDebug("Turbosight: device doesn't have a CI slot");
        return false;
      }
      _isCiSlotPresent = true;

      if (!LoadNewCaApiInstance())
      {
        return false;
      }

      // Attempt to initialise the interface.
      _ciHandle = _onStartCi(_tunerFilter, _tunerFilterName, _apiIndex);
      if (_ciHandle == IntPtr.Zero || _ciHandle.ToInt64() == -1)
      {
        this.LogDebug("Turbosight: interface handle is null");
        _isCiSlotPresent = false;
        return false;
      }

      _mmiMessageBuffer = Marshal.AllocCoTaskMem(MMI_MESSAGE_BUFFER_SIZE);
      _mmiResponseBuffer = Marshal.AllocCoTaskMem(MMI_RESPONSE_BUFFER_SIZE);
      _pmtBuffer = Marshal.AllocCoTaskMem(Pmt.MAX_SIZE + 2);  // + 2 for TBS PMT header
      _mmiMessageQueue = new List<MmiMessage>();
      _isCamPresent = _camAvailable(_ciHandle);
      this.LogDebug("Turbosight: CAM available = {0}", _isCamPresent);

      StartMmiHandlerThread();

      this.LogDebug("Turbosight: result = success");
      return true;
    }

    /// <summary>
    /// Close the conditional access interface.
    /// </summary>
    /// <returns><c>true</c> if the interface is successfully closed, otherwise <c>false</c></returns>
    public bool CloseInterface()
    {
      this.LogDebug("Turbosight: close conditional access interface");

      if (_mmiHandlerThread != null && _mmiHandlerThread.IsAlive)
      {
        _stopMmiHandlerThread = true;
        // In the worst case scenario it should take approximately
        // twice the thread sleep time to cleanly stop the thread.
        _mmiHandlerThread.Join(MMI_HANDLER_THREAD_SLEEP_TIME * 2);
        if (_mmiHandlerThread.IsAlive)
        {
          this.LogDebug("Turbosight: warning, failed to join MMI handler thread => aborting thread");
          _mmiHandlerThread.Abort();
        }
        _mmiHandlerThread = null;
      }

      if (_ciHandle != IntPtr.Zero)
      {
        _onExitCi(_ciHandle);
        _ciHandle = IntPtr.Zero;
      }

      _isCiSlotPresent = false;
      _isCamPresent = false;
      _mmiMessageQueue = null;
      if (_mmiMessageBuffer != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_mmiMessageBuffer);
        _mmiMessageBuffer = IntPtr.Zero;
      }
      if (_mmiResponseBuffer != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_mmiResponseBuffer);
        _mmiResponseBuffer = IntPtr.Zero;
      }
      if (_pmtBuffer != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_pmtBuffer);
        _pmtBuffer = IntPtr.Zero;
      }
      if (_libHandle != IntPtr.Zero)
      {
        NativeMethods.FreeLibrary(_libHandle);
        _libHandle = IntPtr.Zero;
      }
      _dllLoaded = false;

      this.LogDebug("Turbosight: result = success");
      return true;
    }

    /// <summary>
    /// Reset the conditional access interface.
    /// </summary>
    /// <param name="resetDevice">This parameter will be set to <c>true</c> if the device must be reset
    ///   for the interface to be completely and successfully reset.</param>
    /// <returns><c>true</c> if the interface is successfully reset, otherwise <c>false</c></returns>
    public bool ResetInterface(out bool resetDevice)
    {
      this.LogDebug("Turbosight: reset conditional access interface");

      // TBS have confirmed that it is not currently possible to call On_Start_CI() multiple times on a
      // filter instance ***even if On_Exit_CI() is called***. The graph must be rebuilt to reset the CI.
      resetDevice = true;
      return CloseInterface() && OpenInterface();
    }

    /// <summary>
    /// Determine whether the conditional access interface is ready to receive commands.
    /// </summary>
    /// <returns><c>true</c> if the interface is ready, otherwise <c>false</c></returns>
    public bool IsInterfaceReady()
    {
      this.LogDebug("Turbosight: is conditional access interface ready");
      if (!_isCiSlotPresent)
      {
        this.LogDebug("Turbosight: CI slot not present");
        return false;
      }
      if (_ciHandle == IntPtr.Zero)
      {
        this.LogDebug("Turbosight: interface not opened");
        return false;
      }

      // We can only tell whether a CAM is present, not whether it is ready.
      bool camPresent = false;
      lock (this)
      {
        camPresent = _camAvailable(_ciHandle);
      }
      this.LogDebug("Turbosight: result = {0}", camPresent);
      return camPresent;
    }

    /// <summary>
    /// Send a command to to the conditional access interface.
    /// </summary>
    /// <param name="channel">The channel information associated with the service which the command relates to.</param>
    /// <param name="listAction">It is assumed that the interface may be able to decrypt one or more services
    ///   simultaneously. This parameter gives the interface an indication of the number of services that it
    ///   will be expected to manage.</param>
    /// <param name="command">The type of command.</param>
    /// <param name="pmt">The programme map table for the service.</param>
    /// <param name="cat">The conditional access table for the service.</param>
    /// <returns><c>true</c> if the command is successfully sent, otherwise <c>false</c></returns>
    public bool SendCommand(IChannel channel, CaPmtListManagementAction listAction, CaPmtCommand command, Pmt pmt, Cat cat)
    {
      this.LogDebug("Turbosight: send conditional access command, list action = {0}, command = {1}", listAction, command);

      if (!_isTurbosight || _ciHandle == null)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }
      if (pmt == null)
      {
        this.LogDebug("Turbosight: PMT not supplied");
        return true;
      }

      // TBS have a short header at the start of the PMT.
      Marshal.WriteByte(_pmtBuffer, 0, (byte)listAction);
      Marshal.WriteByte(_pmtBuffer, 1, (byte)command);
      int offset = 2;
      ReadOnlyCollection<byte> rawPmt = pmt.GetRawPmt();
      for (int i = 0; i < rawPmt.Count; i++)
      {
        Marshal.WriteByte(_pmtBuffer, offset++, rawPmt[i]);
      }

      //DVB_MMI.DumpBinary(_pmtBuffer, 0, rawPmt.Count + 2);

      _sendPmt(_ciHandle, _pmtBuffer, (ushort)(rawPmt.Count + 2));

      this.LogDebug("Turbosight: result = success");
      return true;
    }

    #endregion

    #region ICiMenuActions members

    /// <summary>
    /// Set the CAM callback handler functions.
    /// </summary>
    /// <param name="ciMenuHandler">A set of callback handler functions.</param>
    /// <returns><c>true</c> if the handlers are set, otherwise <c>false</c></returns>
    public bool SetCiMenuHandler(ICiMenuCallbacks ciMenuHandler)
    {
      if (ciMenuHandler != null)
      {
        _ciMenuCallbacks = ciMenuHandler;
        StartMmiHandlerThread();
        return true;
      }
      return false;
    }

    /// <summary>
    /// Send a request from the user to the CAM to open the menu.
    /// </summary>
    /// <returns><c>true</c> if the request is successfully passed to and processed by the CAM, otherwise <c>false</c></returns>
    public bool EnterCIMenu()
    {
      this.LogDebug("Turbosight: enter menu");

      if (!_isTurbosight || _ciHandle == IntPtr.Zero)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }
      if (!_isCamPresent)
      {
        this.LogDebug("Turbosight: the CAM is not present");
        return false;
      }

      lock (this)
      {
        // Close any existing sessions otherwise the CAM gets confused.
        _mmiMessageQueue.Add(new MmiMessage(TbsMmiMessageType.CloseMmi));
        // We send an "application info" message because attempting to enter the menu will fail
        // if you don't get the application information first.
        _mmiMessageQueue.Add(new MmiMessage(TbsMmiMessageType.ApplicationInfo));
        // The CA information is just for information purposes.
        _mmiMessageQueue.Add(new MmiMessage(TbsMmiMessageType.CaInfo));
        // The main message.
        _mmiMessageQueue.Add(new MmiMessage(TbsMmiMessageType.EnterMenu));
        // We have to request a response.
        _mmiMessageQueue.Add(new MmiMessage(TbsMmiMessageType.GetMmi));
      }
      return true;
    }

    /// <summary>
    /// Send a request from the user to the CAM to close the menu.
    /// </summary>
    /// <returns><c>true</c> if the request is successfully passed to and processed by the CAM, otherwise <c>false</c></returns>
    public bool CloseCIMenu()
    {
      this.LogDebug("Turbosight: close menu");

      if (!_isTurbosight || _ciHandle == IntPtr.Zero)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }
      if (!_isCamPresent)
      {
        this.LogDebug("Turbosight: the CAM is not present");
        return false;
      }

      lock (this)
      {
        _mmiMessageQueue.Add(new MmiMessage(TbsMmiMessageType.CloseMmi));
      }
      return true;
    }

    /// <summary>
    /// Send a menu entry selection from the user to the CAM.
    /// </summary>
    /// <param name="choice">The index of the selection as an unsigned byte value.</param>
    /// <returns><c>true</c> if the selection is successfully passed to and processed by the CAM, otherwise <c>false</c></returns>
    public bool SelectMenu(byte choice)
    {
      this.LogDebug("Turbosight: select menu entry, choice = {0}", choice);

      if (!_isTurbosight || _ciHandle == IntPtr.Zero)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }
      if (!_isCamPresent)
      {
        this.LogDebug("Turbosight: the CAM is not present");
        return false;
      }

      lock (this)
      {
        MmiMessage selectMessage = new MmiMessage(TbsMmiMessageType.MenuAnswer, 3);
        selectMessage.Message[0] = 0;
        selectMessage.Message[1] = 0;
        selectMessage.Message[2] = choice;
        _mmiMessageQueue.Add(selectMessage);
        // Don't explicitly request a response for a "back" request as that
        // could choke the message queue with a message that the CAM
        // never answers.
        if (choice != 0)
        {
          _mmiMessageQueue.Add(new MmiMessage(TbsMmiMessageType.GetMmi));
        }
      }
      return true;
    }

    /// <summary>
    /// Send a response from the user to the CAM.
    /// </summary>
    /// <param name="cancel"><c>True</c> to cancel the request.</param>
    /// <param name="answer">The user's response.</param>
    /// <returns><c>true</c> if the response is successfully passed to and processed by the CAM, otherwise <c>false</c></returns>
    public bool SendMenuAnswer(bool cancel, string answer)
    {
      if (answer == null)
      {
        answer = string.Empty;
      }
      this.LogDebug("Turbosight: send menu answer, answer = {0}, cancel = {1}", answer, cancel);

      if (!_isTurbosight || _ciHandle == IntPtr.Zero)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }
      if (!_isCamPresent)
      {
        this.LogDebug("Turbosight: the CAM is not present");
        return false;
      }

      if (answer.Length > 254)
      {
        this.LogDebug("Turbosight: answer too long, length = {0}", answer.Length);
        return false;
      }

      byte responseType = (byte)MmiResponseType.Answer;
      if (cancel)
      {
        responseType = (byte)MmiResponseType.Cancel;
      }
      lock (this)
      {
        MmiMessage answerMessage = new MmiMessage(TbsMmiMessageType.Answer, answer.Length + 3);
        answerMessage.Message[0] = (byte)(answer.Length + 1);
        answerMessage.Message[1] = 0;
        answerMessage.Message[2] = responseType;
        int offset = 3;
        for (int i = 0; i < answer.Length; i++)
        {
          answerMessage.Message[offset++] = (byte)answer[i];
        }
        _mmiMessageQueue.Add(answerMessage);
        // We have to request a response.
        _mmiMessageQueue.Add(new MmiMessage(TbsMmiMessageType.GetMmi));
      }
      return true;
    }

    #endregion

    #region IDiseqcDevice members

    /// <summary>
    /// Send a tone/data burst command, and then set the 22 kHz continuous tone state.
    /// </summary>
    /// <param name="toneBurstState">The tone/data burst command to send, if any.</param>
    /// <param name="tone22kState">The 22 kHz continuous tone state to set.</param>
    /// <returns><c>true</c> if the tone state is set successfully, otherwise <c>false</c></returns>
    public bool SetToneState(ToneBurst toneBurstState, Tone22k tone22kState)
    {
      this.LogDebug("Turbosight: set tone state, burst = {0}, 22 kHz = {1}", toneBurstState, tone22kState);

      if (!_isTurbosight || _propertySet == null)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }

      TbsAccessParams accessParams = new TbsAccessParams();
      accessParams.AccessMode = TbsAccessMode.Tone;
      bool success = true;
      int hr;

      // Send the burst command first.
      if (toneBurstState != ToneBurst.None)
      {
        accessParams.Tone = TbsTone.BurstUnmodulated;
        if (toneBurstState == ToneBurst.DataBurst)
        {
          accessParams.Tone = TbsTone.BurstModulated;
        }

        Marshal.StructureToPtr(accessParams, _generalBuffer, true);
        //DVB_MMI.DumpBinary(_generalBuffer, 0, TBS_ACCESS_PARAMS_SIZE);

        hr = _propertySet.Set(_propertySetGuid, _tbsAccessProperty,
          _generalBuffer, TBS_ACCESS_PARAMS_SIZE,
          _generalBuffer, TBS_ACCESS_PARAMS_SIZE
        );
        if (hr == (int)HResult.Severity.Success)
        {
          this.LogDebug("Turbosight: burst result = success");
        }
        else
        {
          this.LogDebug("Turbosight: burst result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
          success = false;
        }
      }

      // Now set the 22 kHz tone state.
      accessParams.Tone = TbsTone.Off;
      if (tone22kState == Tone22k.On)
      {
        accessParams.Tone = TbsTone.On;
      }

      Marshal.StructureToPtr(accessParams, _generalBuffer, true);
      //DVB_MMI.DumpBinary(_generalBuffer, 0, TBS_ACCESS_PARAMS_SIZE);

      hr = _propertySet.Set(_propertySetGuid, _tbsAccessProperty,
        _generalBuffer, TBS_ACCESS_PARAMS_SIZE,
        _generalBuffer, TBS_ACCESS_PARAMS_SIZE
      );
      if (hr == (int)HResult.Severity.Success)
      {
        this.LogDebug("Turbosight: 22 kHz result = success");
      }
      else
      {
        this.LogDebug("Turbosight: 22 kHz result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
        success = false;
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
      this.LogDebug("Turbosight: send DiSEqC command");

      if (!_isTurbosight || _propertySet == null)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }
      if (command == null || command.Length == 0)
      {
        this.LogDebug("Turbosight: command not supplied");
        return true;
      }
      if (command.Length > MAX_DISEQC_MESSAGE_LENGTH)
      {
        this.LogDebug("Turbosight: command too long, length = {0}", command.Length);
        return false;
      }

      TbsAccessParams accessParams = new TbsAccessParams();
      accessParams.AccessMode = TbsAccessMode.Diseqc;
      accessParams.DiseqcTransmitMessageLength = (uint)command.Length;
      accessParams.DiseqcTransmitMessage = new byte[MAX_DISEQC_MESSAGE_LENGTH];
      Buffer.BlockCopy(command, 0, accessParams.DiseqcTransmitMessage, 0, command.Length);

      Marshal.StructureToPtr(accessParams, _generalBuffer, true);
      //DVB_MMI.DumpBinary(_generalBuffer, 0, TBS_ACCESS_PARAMS_SIZE);

      int hr = _propertySet.Set(_propertySetGuid, _tbsAccessProperty,
        _generalBuffer, TBS_ACCESS_PARAMS_SIZE,
        _generalBuffer, TBS_ACCESS_PARAMS_SIZE
      );
      if (hr == (int)HResult.Severity.Success)
      {
        this.LogDebug("Turbosight: result = success");
        return true;
      }

      this.LogDebug("Turbosight: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
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
      this.LogDebug("Turbosight: read DiSEqC response");
      response = null;

      if (!_isTurbosight || _propertySet == null)
      {
        this.LogDebug("Turbosight: device not initialised or interface not supported");
        return false;
      }

      for (int i = 0; i < TBS_ACCESS_PARAMS_SIZE; i++)
      {
        Marshal.WriteByte(_generalBuffer, i, 0);
      }

      TbsAccessParams accessParams = new TbsAccessParams();
      accessParams.AccessMode = TbsAccessMode.Diseqc;
      int returnedByteCount;
      int hr = _propertySet.Get(_propertySetGuid, _tbsAccessProperty,
        _generalBuffer, TBS_ACCESS_PARAMS_SIZE,
        _generalBuffer, TBS_ACCESS_PARAMS_SIZE,
        out returnedByteCount
      );
      if (hr != (int)HResult.Severity.Success)
      {
        this.LogDebug("Turbosight: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
        return false;
      }

      DVB_MMI.DumpBinary(_generalBuffer, 0, returnedByteCount);

      if (returnedByteCount != TBS_ACCESS_PARAMS_SIZE)
      {
        this.LogDebug("Turbosight: result = failure, unexpected number of bytes ({0}) returned", returnedByteCount);
        return false;
      }

      accessParams = (TbsAccessParams)Marshal.PtrToStructure(_generalBuffer, typeof(TbsAccessParams));
      if (accessParams.DiseqcReceiveMessageLength > MAX_DISEQC_MESSAGE_LENGTH)
      {
        this.LogDebug("Turbosight: result = failure, unexpected number of message bytes ({0}) returned", accessParams.DiseqcReceiveMessageLength);
        return false;
      }
      response = new byte[accessParams.DiseqcReceiveMessageLength];
      Buffer.BlockCopy(accessParams.DiseqcReceiveMessage, 0, response, 0, (int)accessParams.DiseqcReceiveMessageLength);
      this.LogDebug("Turbosight: result = success");
      return true;
    }

    #endregion

    #region IDisposable member

    /// <summary>
    /// Release and dispose all resources.
    /// </summary>
    public override void Dispose()
    {
      CloseInterface();
      if (_generalBuffer != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_generalBuffer);
        _generalBuffer = IntPtr.Zero;
      }
      if (_isUsb)
      {
        Release.ComObject("Turbosight property set", ref _propertySet);
      }
      _tunerFilter = null;
      _isTurbosight = false;
    }

    #endregion
  }
}
