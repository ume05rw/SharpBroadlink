using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBroadlink.Devices
{
    /// <summary>
    /// Rm2Pro - Programmable Remote Controller for RF
    /// </summary>
    public class Rm2Pro : Rm
    {
        #region Properties

        public static TimeSpan RfFrequencyLearnInterval { get; set; } = TimeSpan.FromMilliseconds(1000);
        public static TimeSpan RfCommandLearnInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="mac"></param>
        /// <param name="devType"></param>
        public Rm2Pro(IPEndPoint host, byte[] mac, int devType) : base(host, mac, devType)
        {
            DeviceType = DeviceType.Rm2Pro;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Async method for learning RF commands
        /// </summary>
        /// <remarks>User should press and hold remote button until learning is finished</remarks>
        /// <param name="learnInstructions">When this action is invoked, instructions should be show to the user</param>
        /// <param name="cancellationToken">Used to cancel RF learning</param>
        /// <returns>Learned RF command</returns>
        /// <exception cref="InvalidOperationException">In case of failure</exception>
        /// <exception cref="TaskCanceledException">In case learning has benn cancelled</exception>
        public async Task<byte[]> LearnRfCommand(Action<RfLearningSteps> learnInstructions, CancellationToken cancellationToken, Action learnProgress = null)
        {            
            return await Task.Run(async () =>
            {
                try
                {
                    if (!await CancelLearning())
                        throw new InvalidOperationException("Failed to cancel any previous learning");
                    learnInstructions?.Invoke(RfLearningSteps.SweepingFrequencies);

                    if (!await SweepFrequencies())
                        throw new InvalidOperationException("Failed to sweep frequencies");

                    //Learn RF frequency - remote should be held all the time
                    while (!await LearnRfFrequency())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        learnProgress?.Invoke();
                        await Task.Delay(RfFrequencyLearnInterval, cancellationToken);
                    }

                    learnInstructions?.Invoke(RfLearningSteps.SweepingFrequenciesFinished);

                    //Move to step 2 - learning the actual command
                    await FindRfPacket();
                    learnInstructions?.Invoke(RfLearningSteps.LearningCommand);

                    byte[] commandData = null;
                    while(null == (commandData = await CheckData()))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        learnProgress?.Invoke();
                        await Task.Delay(RfCommandLearnInterval, cancellationToken);
                    }
                    return commandData;
                }
                finally
                {
                    await CancelLearning();
                }
            }, cancellationToken);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Switch into RF Learning mode
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SweepFrequencies()
        {
            var packet = new byte[16];
            packet[0] = 0x19;

            await SendPacket(0x6a, packet);

            return true;
        }

        /// <summary>
        /// Enter RF frequency learning
        /// </summary>
        /// <returns>True if entering learning was successful</returns>
        private async Task<bool> LearnRfFrequency()
        {
            var packet = new byte[16];
            packet[0] = 0x1a;

            var response = await SendPacket(0x6a, packet);
            if (response == null || response.Length <= 0x38)
                return false;

            var err = response[0x22] | (response[0x23] << 8);

            if (err == 0)
            {
                var payload = Decrypt(response, 0x38);
                if (payload.Length <= 0x04)
                    return false;

                return payload[0x04] == 1;
            }

            // failure
            return false;
        }

        /// <summary>
        /// Starts RF packet discovery
        /// </summary>
        /// <remarks>Return value doesn't seem to be correct, it's always incorrect</remarks>
        /// <returns>Ignore?</returns>
        private async Task<bool> FindRfPacket()
        {
            var packet = new byte[16];
            packet[0] = 0x1b;

            var response = await SendPacket(0x6a, packet);
            if (response == null || response.Length <= 0x39)
                return false;

            var err = response[0x22] | (response[0x23] << 8);
            if (err == 0)
            {
                var payload = Decrypt(response, 0x38);
                if (payload.Length < 5)
                    return false;
                return payload[0x04] == 1;
            }

            return false;
        }

        #endregion Private Methods

        #region Types

        public enum RfLearningSteps
        {
            [Description("Press and hold the remote button")]
            SweepingFrequencies,
            [Description("Release the remote")]
            SweepingFrequenciesFinished,
            [Description("Press again shortly the command you want to learn")]
            LearningCommand,
        }

        #endregion Types
    }
}
