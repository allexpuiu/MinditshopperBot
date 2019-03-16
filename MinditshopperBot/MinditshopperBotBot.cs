// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;

namespace MinditshopperBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class MinditshopperBotBot : IBot
    {
        private readonly MinditshopperBotAccessors _accessors;
        private readonly ILogger _logger;
        private readonly RecommenderClient recommenderClient;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="conversationState">The managed conversation state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public MinditshopperBotBot(ConversationState conversationState, ILoggerFactory loggerFactory, RecommenderClient recommenderClient)
        {
            if (conversationState == null)
            {
                throw new System.ArgumentNullException(nameof(conversationState));
            }

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            if (recommenderClient == null)
            {
                throw new System.ArgumentNullException(nameof(recommenderClient));
            } else
            {
                this.recommenderClient = recommenderClient;
            }

            _accessors = new MinditshopperBotAccessors(conversationState)
            {
                MindshopperUserState = conversationState.CreateProperty<MindshopperUserState>(MinditshopperBotAccessors.MinditshoperUserState),
            };

            _logger = loggerFactory.CreateLogger<MinditshopperBotBot>();
            _logger.LogTrace("Turn start.");
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            var state = await _accessors.MindshopperUserState.GetAsync(turnContext, () => new MindshopperUserState());

            if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                _logger.LogInformation("Starting a conversation");
                StartConversation(state, turnContext, cancellationToken);
            }

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                switch (state.TurnCount) {
                    case State.START:
                       HelloUser1(state, turnContext, cancellationToken);
                       break;
                    case State.CHOOSE_CATEGORY:
                        ChooseCategory(state, turnContext, cancellationToken);
                        break;
                    default:
                        state.TurnCount++;
                        await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                        // Save the new turn count into the conversation state.
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);

                        // Echo back to the user whatever they typed.
                        var responseMessage = $"Turn {state.TurnCount}: Hello '{state.Name}'";
                        await turnContext.SendActivityAsync(responseMessage);
                        break;
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartConversation(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            /*
            IConversationUpdateActivity update = turnContext.Activity;
            
            if (update.MembersAdded != null && update.MembersAdded.Any())
            {
                foreach (var newMember in update.MembersAdded)
                {
                    if (newMember.Id != turnContext.Activity.Recipient.Id)
                    {
                        if (turnContext.Activity.From.Properties["userId"] != null)
                        {
                            state.UserId = turnContext.Activity.From.Properties["userId"].ToString();
                        }

                        if (turnContext.Activity.From.Properties["cartId"] != null)
                        {
                            state.CartId = turnContext.Activity.From.Properties["cartId"].ToString();
                        }

                        if (turnContext.Activity.From.Properties["name"] != null)
                        {
                            state.Name = turnContext.Activity.From.Properties["name"].ToString();
                        }

                        state.TurnCount = State.CHOOSE_CATEGORY;

                        await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);


                        string hello = $"Hello, '{state.Name}'. I am your personal shopping assistant and I will guide you during the shopping process." +
                            $"\n" +
                            $"\n Please select what you want to buy:" +
                            $"\n\t\t1) Tobacco" +
                            $"\n\t\t2) Food" +
                            $"\n\t\t3) Perfumes & Cosmetics" +
                            $"\n\t\t4) Liquor";

                        await turnContext.SendActivityAsync(hello);
                    }
                }
            }*/


            if (turnContext.Activity.From.Properties["userId"] != null)
            {
                state.UserId = turnContext.Activity.From.Properties["userId"].ToString();
            }

            if (turnContext.Activity.From.Properties["cartId"] != null)
            {
                state.CartId = turnContext.Activity.From.Properties["cartId"].ToString();
            }

            if (turnContext.Activity.From.Properties["name"] != null)
            {
                state.Name = turnContext.Activity.From.Properties["name"].ToString();
            }

            state.TurnCount = State.CHOOSE_CATEGORY;

            await _accessors.MindshopperUserState.SetAsync(turnContext, state);
            await _accessors.ConversationState.SaveChangesAsync(turnContext);


            string hello = $"Hello, '{state.Name}'. I am your personal shopping assistant and I will guide you during the shopping process." +
                $"\n" +
                $"\n Please select what you want to buy:" +
                $"\n\t\t1) Tobacco" +
                $"\n\t\t2) Food" +
                $"\n\t\t3) Perfumes & Cosmetics" +
                $"\n\t\t4) Liquor";

            await turnContext.SendActivityAsync(hello);
        }

        public async Task ChooseCategory(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var response = turnContext.Activity.Text;

            if (response != null && response.Contains("1"))
            {
                IList<Item> list =  RecommenderClient.ProcessTopItems("10");

                string text = $"Following items are top sellers in the category: 10\n";
                int cnt = 1;
                foreach (Item i in list)
                {
                    text += $"\n\t\t '{cnt}' - " + i.ItemName;
                }
                text += "\n Choose the item";

                state.TurnCount = State.SELECTED_CATEGORY_ITEM;

                await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await turnContext.SendActivityAsync(text);
            }

            //TODO add for other categories
        }

        public async Task ChooseItemCategory(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var response = turnContext.Activity.Text;

            if (response != null && response.Contains("1"))
            {
                IList<Item> list = RecommenderClient.ProcessTopItems("10");

                string text = $"Following items are top sellers in the category: 10\n";
                int cnt = 1;
                foreach (Item i in list)
                {
                    text += $"\n\t\t '{cnt}' - " + i.ItemName;
                }
                text += "\n Choose the item";

                state.TurnCount = State.SELECTED_CATEGORY_ITEM;

                await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await turnContext.SendActivityAsync(text);
            }

            //TODO add for other categories
        }

        public async Task ChooseItemCategory(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var response = turnContext.Activity.Text;

            if (response != null && response.Contains("1"))
            {
                IList<Item> list = RecommenderClient.ProcessTopItems("10");

                string text = $"Following items are top sellers in the category: 10\n";
                int cnt = 1;
                foreach (Item i in list)
                {
                    text += $"\n\t\t '{cnt}' - " + i.ItemName;
                }
                text += "\n Choose the item";

                state.TurnCount = State.SELECTED_CATEGORY_ITEM;

                await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await turnContext.SendActivityAsync(text);
            }

            //TODO add for other categories
        }

        public async Task HelloUser1(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            state.TurnCount = State.CHOOSE_CATEGORY;
            state.UserId = "Dumi";

            // Set the property using the accessor.
            await _accessors.MindshopperUserState.SetAsync(turnContext, state);
            // Save the new turn count into the conversation state.
            await _accessors.ConversationState.SaveChangesAsync(turnContext);

            // Echo back to the user whatever they typed.
            var responseMessage = $"Turn {state.TurnCount}: Hello '{state.Name}'";

            
            await turnContext.SendActivityAsync(responseMessage);
        }
    }
}
