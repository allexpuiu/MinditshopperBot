// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
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
        private string lastItemProcessed = "";

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
                state.UserId = "1";
                state.CartId = "1";
                state.Name = "Dumi";
                cleanCarts();
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
                    case State.SELECTED_CATEGORY_ITEM:
                        SelectedCategoryItem(state, turnContext, cancellationToken);
                        break;
                    case State.CHOOSE_RECOMMENDED_ITEM:
                        ChooseRecommendedItem(state, turnContext, cancellationToken);
                        break;
                    case State.SELECTED_RECOMMENDED_ITEM:
                        SelectedRecommendedItem(state, turnContext, cancellationToken);
                        break;
                    case State.END:
                        //TODO implemente here the method that closes the cart at client
                        CloseCart(state, turnContext, cancellationToken);
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
            //else
            //{
            //    await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            //}
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

            state.LastProcessedItem = "";

            string hello = $"Hello, '{state.Name}'. I am your personal shopping assistant and I will guide you during the shopping process." +
                $"\n" +
                $"\n Please select what you want to buy:" +
                $"\n\t\t1) Tobacco" +
                $"\n\t\t2) Liquor" +
                $"\n\t\t3) Food" +
                $"\n\t\t4) Perfumes & Cosmetics";

            await turnContext.SendActivityAsync(hello);
        }

        public async Task SelectedCategoryItem(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            ;
            var response = turnContext.Activity.Text;
            
            string text = "";
            var item = RecommenderClient.ProcessItem(response);
            if (item != null)
            {
                state.SelectedItems.Add(item);
                text = $"You have choosen {response}. Type \"OK\" to continue.";
                state.LastProcessedItem = response;
                state.TurnCount = State.CHOOSE_RECOMMENDED_ITEM;
            } else
            {
                text = $"You have choosen an invalid item. Type \"OK\" to continue and select a correct item.";
                state.TurnCount = State.CHOOSE_CATEGORY_ITEM;
            }

            await _accessors.MindshopperUserState.SetAsync(turnContext, state);
            await _accessors.ConversationState.SaveChangesAsync(turnContext);
            await turnContext.SendActivityAsync(text);
        }

        public async Task SelectedRecommendedItem(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            string text = "";
            var response = turnContext.Activity.Text;
            if(response != null)
            {
                if (!response.ToLower().Contains("no"))
                {
                    var item = RecommenderClient.ProcessItem(response);
                    if (item != null)
                    {
                        state.SelectedItems.Add(item);
                        text = $"You have choosen {response}. Type \"OK\" to continue.";
                        state.LastProcessedItem = response;
                        state.TurnCount = State.CHOOSE_RECOMMENDED_ITEM;
                    }
                    else
                    {
                        text = $"You have choosen an invalid item. Type \"OK\" to continue and select a correct item.";
                        state.TurnCount = State.CHOOSE_CATEGORY_ITEM;
                    }
                } else
                {
                    state.TurnCount = State.CHOOSE_CATEGORY;
                    text = $"You are being sent back to the category choosing.\n Please select the category of interest:" +
                $"\n\t\t1) Tobacco" +
                $"\n\t\t2) Liquor" +
                $"\n\t\t3) Food" +
                $"\n\t\t4) Perfumes & Cosmetics" +
                $"\n\t\tType\"none\" to close the cart.";
                }
                
            }

            await _accessors.MindshopperUserState.SetAsync(turnContext, state);
            await _accessors.ConversationState.SaveChangesAsync(turnContext);
            await turnContext.SendActivityAsync(text);
        }

        public async Task CloseCart(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            builder.DataSource = "mindshopper.database.windows.net";
            builder.UserID = "mindshopper";
            builder.Password = "8799LipYAA9oksRLG6ia";
            builder.InitialCatalog = "mindshopper";


            try
            {
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    foreach (Item i in state.SelectedItems)
                    { 
                        StringBuilder sb = new StringBuilder();
                        sb.Append("INSERT INTO [dbo].[item_cart] (id,item_id, item_description, price, quantity, category_id, category_description, cart_id) VALUES");
                        sb.Append($"(NEXT VALUE FOR Hibernate_Sequence,'{i.ItemId}', '{i.ItemName}', {i.SalesValue/100}, 1, '{i.CategoryCode}', '{i.Category}', {state.CartId})");
                        String sql = sb.ToString();

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }

                    StringBuilder sb2 = new StringBuilder();
                    sb2.Append("UPDATE [dbo].[cart] SET STATUS = 'COMPLETED'");
                    sb2.Append($"where id = {state.CartId}");
                    String sql2 = sb2.ToString();

                    using (SqlCommand command = new SqlCommand(sql2, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            await _accessors.MindshopperUserState.SetAsync(turnContext, state);
            await _accessors.ConversationState.SaveChangesAsync(turnContext);
        }

        private void cleanCarts()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            builder.DataSource = "mindshopper.database.windows.net";
            builder.UserID = "mindshopper";
            builder.Password = "8799LipYAA9oksRLG6ia";
            builder.InitialCatalog = "mindshopper";
            Item item = null; ;


            try
            {
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    
                    StringBuilder sb2 = new StringBuilder();
                    sb2.Append("DELETE FROM [dbo].[cart];");
                    sb2.Append("insert into dbo.cart (id, status, date_created, user_id) values (1, 'NEW', GETDATE(), 1);");
                    String sql2 = sb2.ToString();

                    using (SqlCommand command = new SqlCommand(sql2, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task ChooseCategory(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var response = turnContext.Activity.Text;

            if (response != null && response.Contains("1"))
            {
                IList<Item> list =  RecommenderClient.ProcessTopItems("10");

                string text = $"Following items are top sellers in the category: 10\n";
                foreach (Item i in list)
                {
                    text += $"\n\t\t '{i.ItemId}' - " + i.ItemName;
                }
                text += "\n Choose the item";

                state.TurnCount = State.SELECTED_CATEGORY_ITEM;

                await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await turnContext.SendActivityAsync(text);

            } else if (response != null && response.Contains("2"))
            {
                IList<Item> list = RecommenderClient.ProcessTopItems("20");

                string text = $"Following items are top sellers in the category: 20\n";
                foreach (Item i in list)
                {
                    text += $"\n\t\t '{i.ItemId}' - " + i.ItemName;
                }
                text += "\n Choose the item";

                state.TurnCount = State.SELECTED_CATEGORY_ITEM;

                await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await turnContext.SendActivityAsync(text);
            } else if (response != null && response.Contains("3"))
            {
                IList<Item> list = RecommenderClient.ProcessTopItems("30");

                string text = $"Following items are top sellers in the category: 30\n";
                foreach (Item i in list)
                {
                    text += $"\n\t\t '{i.ItemId}' - " + i.ItemName;
                }
                text += "\n Choose the item";

                state.TurnCount = State.SELECTED_CATEGORY_ITEM;

                await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await turnContext.SendActivityAsync(text);
            } else if (response != null && response.Contains("4"))
            {
                IList<Item> list = RecommenderClient.ProcessTopItems("40");

                string text = $"Following items are top sellers in the category: 40\n";
                foreach (Item i in list)
                {
                    text += $"\n\t\t '{i.ItemId}' - " + i.ItemName;
                }
                text += "\n Choose the item";

                state.TurnCount = State.SELECTED_CATEGORY_ITEM;

                await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await turnContext.SendActivityAsync(text);
            } else if (response != null && response.ToLower().Contains("no"))
            {
                string text = $"Thank you for buying. Have a nice day! Type \"OK\" to confirm the completion of the cart.";
                state.TurnCount = State.END;

                await _accessors.MindshopperUserState.SetAsync(turnContext, state);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await turnContext.SendActivityAsync(text);
            }

        }

        public async Task ChooseRecommendedItem(MindshopperUserState state, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var response = turnContext.Activity.Text;
            string text = "";
            if (response != null)
            {
                if (!response.ToLower().Contains("no"))
                {
                    response = response.ToLower().Equals("ok") ? state.LastProcessedItem : response;

                    IList<Item> list = RecommenderClient.ProcessRecommendedItem(response);

                    text = $"Following items are recommended to you based on the current items in your cart:\n";
                    foreach (Item i in list)
                    {
                        text += $"\n\t\t '{i.ItemId}' - " + i.ItemName;
                    }
                    text += "\n Choose the item, otherwise, type \"none\".";

                    state.TurnCount = State.SELECTED_RECOMMENDED_ITEM;


                } else
                {
                    state.TurnCount = State.CHOOSE_CATEGORY;
                }
            } else
            {
                state.TurnCount = State.CHOOSE_RECOMMENDED_ITEM;
            } 

            await _accessors.MindshopperUserState.SetAsync(turnContext, state);
            await _accessors.ConversationState.SaveChangesAsync(turnContext);
            await turnContext.SendActivityAsync(text);
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
