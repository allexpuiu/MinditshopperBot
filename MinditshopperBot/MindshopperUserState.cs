// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MinditshopperBot
{
    /// <summary>
    /// Stores counter state for the conversation.
    /// Stored in <see cref="Microsoft.Bot.Builder.ConversationState"/> and
    /// backed by <see cref="Microsoft.Bot.Builder.MemoryStorage"/>.
    /// </summary>
    public class MindshopperUserState
    {
        /// <summary>
        /// Gets or sets the number of turns in the conversation.
        /// </summary>
        /// <value>The number of turns in the conversation.</value>
        public State TurnCount { get; set; } = 0;

        public string UserId { get; set; } = "N/A";

        public string CartId { get; set; } = "-1";

        public string Name { get; set; } = "Unknown User";

        public bool WasWelcomed { get; set; } = false;

        public string LastProcessedItem { get; set; }
    }
}
