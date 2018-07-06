﻿using System;
using NiceHashMinerLegacy.Common.Utils;
using NiceHashMinerLegacy.Devices;

namespace NiceHashMinerLegacy.Windows.Device
{
    public class AmdComputeDevice : Devices.Device.AmdComputeDevice
    {
        private readonly int _adapterIndex; // For ADL
        private readonly int _adapterIndex2; // For ADL2
        private readonly IntPtr _adlContext;
        private bool _powerHasFailed;

        public override int FanSpeed
        {
            get
            {
                var adlf = new ADLFanSpeedValue
                {
                    SpeedType = ADL.ADL_DL_FANCTRL_SPEED_TYPE_RPM
                };
                var result = ADL.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref adlf);
                if (result != ADL.ADL_SUCCESS)
                {
                    Helpers.ConsolePrint("ADL", "ADL fan getting failed with error code " + result);
                    return -1;
                }
                return adlf.FanSpeed;
            }
        }

        public override float Temp
        {
            get
            {
                var adlt = new ADLTemperature();
                var result = ADL.ADL_Overdrive5_Temperature_Get(_adapterIndex, 0, ref adlt);
                if (result != ADL.ADL_SUCCESS)
                {
                    Helpers.ConsolePrint("ADL", "ADL temp getting failed with error code " + result);
                    return -1;
                }
                return adlt.Temperature * 0.001f;
            }
        }

        public override float Load
        {
            get
            {
                var adlp = new ADLPMActivity();
                var result = ADL.ADL_Overdrive5_CurrentActivity_Get(_adapterIndex, ref adlp);
                if (result != ADL.ADL_SUCCESS)
                {
                    Helpers.ConsolePrint("ADL", "ADL load getting failed with error code " + result);
                    return -1;
                }
                return adlp.ActivityPercent;
            }
        }

        public override double PowerUsage
        {
            get
            {
                var power = -1;
                if (!_powerHasFailed && _adlContext != IntPtr.Zero && ADL.ADL2_Overdrive6_CurrentPower_Get != null)
                {
                    var result = ADL.ADL2_Overdrive6_CurrentPower_Get(_adlContext, _adapterIndex2, 1, ref power);
                    if (result == ADL.ADL_SUCCESS)
                    {
                        return (double) power / (1 << 8);
                    }

                    // Only alert once
                    Helpers.ConsolePrint("ADL", $"ADL power getting failed with code {result} for GPU {NameCount}. Turning off power for this GPU.");
                    _powerHasFailed = true;
                }

                return power;
            }
        }

        internal AmdComputeDevice(AmdGpuDevice amdDevice, int gpuCount, bool isDetectionFallback, int adl2Index)
            : base(amdDevice, gpuCount, isDetectionFallback)
        {
            _adapterIndex = amdDevice.AdapterIndex;
            ADL.ADL2_Main_Control_Create?.Invoke(ADL.ADL_Main_Memory_Alloc, 0, ref _adlContext);
            _adapterIndex2 = adl2Index;
        }
    }
}
