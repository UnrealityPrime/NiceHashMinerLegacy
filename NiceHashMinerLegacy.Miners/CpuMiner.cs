﻿using System.Diagnostics;
using System.Threading.Tasks;
using NiceHashMinerLegacy.Common.Enums;
using NiceHashMinerLegacy.Common.Interfaces;
using NiceHashMinerLegacy.Common.Models;
using NiceHashMinerLegacy.Common.Utils;
using NiceHashMinerLegacy.Devices;
using NiceHashMinerLegacy.Devices.Algorithms;
using NiceHashMinerLegacy.Miners.Parsing;

namespace NiceHashMinerLegacy.Miners
{
    public class CpuMiner : Miner
    {
        public CpuMiner()
            : base("cpuminer_CPU")
        {
        }

        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 3600000; // 1hour
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            if (!IsInit)
            {
                Helpers.ConsolePrint(MinerTag(), "MiningSetup is not initialized exiting Start()");
                return;
            }

            var username = GetUsername(btcAdress, worker);

            LastCommandLine = "--algo=" + MiningSetup.MinerName +
                              " --url=" + url +
                              " --userpass=" + username + ":x " +
                              ExtraLaunchParametersParser.ParseForMiningSetup(
                                  MiningSetup,
                                  DeviceType.CPU) +
                              " --api-bind=" + ApiPort;

            ProcessHandle = _Start();
        }

        public override Task<ApiData> GetSummaryAsync()
        {
            return GetSummaryCpuCcminerAsync();
        }

        protected override void _Stop(MinerStopType willswitch)
        {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        protected override INHProcess _Start()
        {
            var p = base._Start();

            var affinityMask = MiningSetup.MiningPairs[0].Device.AffinityMask;
            if (affinityMask != 0 && p != null)
                ComputeDeviceManager.CpuAdjuster?.AdjustAffinity(p.Id, affinityMask);

            return p;
        }

        // new decoupled benchmarking routines

        #region Decoupled benchmarking routines

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            return "--algo=" + algorithm.MinerName +
                   " --benchmark" +
                   ExtraLaunchParametersParser.ParseForMiningSetup(
                       MiningSetup,
                       DeviceType.CPU) +
                   " --time-limit " + time;
        }

        protected override Process BenchmarkStartProcess(string CommandLine)
        {
            var benchmarkHandle = base.BenchmarkStartProcess(CommandLine);

            var affinityMask = MiningSetup.MiningPairs[0].Device.AffinityMask;
            if (affinityMask != 0 && benchmarkHandle != null)
                ComputeDeviceManager.CpuAdjuster?.AdjustAffinity(benchmarkHandle.Id, affinityMask);

            return benchmarkHandle;
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
            if (!double.TryParse(outdata, out var lastSpeed)) return false;

            BenchmarkAlgorithm.BenchmarkSpeed = lastSpeed;
            return true;

        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            CheckOutdata(outdata);
        }

        #endregion // Decoupled benchmarking routines
    }
}
