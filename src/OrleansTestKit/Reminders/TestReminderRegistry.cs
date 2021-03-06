﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Orleans.Runtime;
using Orleans.Timers;

namespace Orleans.TestKit.Reminders
{
    public sealed class TestReminderRegistry :
        IReminderRegistry
    {
        private IRemindable _grain;

        private readonly Dictionary<string, TestReminder> _reminders = new Dictionary<string, TestReminder>();

        internal void SetGrainTarget(IRemindable grain) =>
            _grain = grain ?? throw new ArgumentNullException(nameof(grain));

        public Mock<IReminderRegistry> Mock { get; } = new Mock<IReminderRegistry>();

        public async Task<IGrainReminder> GetReminder(string reminderName)
        {
            if (reminderName == null)
            {
                throw new ArgumentNullException(nameof(reminderName));
            }

            await Mock.Object.GetReminder(reminderName).ConfigureAwait(false);
            return !_reminders.TryGetValue(reminderName, out var reminder) ? null : reminder;
        }

        public async Task<List<IGrainReminder>> GetReminders()
        {
            await Mock.Object.GetReminders().ConfigureAwait(false);
            return _reminders.Values.ToList<IGrainReminder>();
        }

        public async Task<IGrainReminder> RegisterOrUpdateReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName == null)
            {
                throw new ArgumentNullException(nameof(reminderName));
            }

            await Mock.Object.RegisterOrUpdateReminder(reminderName, dueTime, period).ConfigureAwait(false);
            var reminder = new TestReminder(reminderName, dueTime, period);
            _reminders[reminderName] = reminder;
            return reminder;
        }

        public async Task UnregisterReminder(IGrainReminder reminder)
        {
            if (reminder == null)
            {
                throw new ArgumentNullException(nameof(reminder));
            }

            await Mock.Object.UnregisterReminder(reminder).ConfigureAwait(false);
            _reminders.Remove(reminder.ReminderName);
        }

        public Task FireReminder(string reminderName, TickStatus tickStatus)
        {
            if (reminderName == null)
            {
                throw new ArgumentNullException(nameof(reminderName));
            }

            if (!_reminders.ContainsKey(reminderName))
            {
                throw new ArgumentException($"No reminder named {reminderName} found");
            }

            return _grain.ReceiveReminder(reminderName, tickStatus);
        }

        public async Task FireAllReminders(TickStatus tickStatus)
        {
            foreach (var reminderName in _reminders.Keys)
            {
                await _grain.ReceiveReminder(reminderName, tickStatus).ConfigureAwait(false);
            }
        }
    }
}
