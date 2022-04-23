﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SwipeControl_TestUI;

using IconSource = Microsoft.UI.Xaml.Controls.IconSource;
using SwipeItem = Microsoft.UI.Xaml.Controls.SwipeItem;
using SwipeControl = Microsoft.UI.Xaml.Controls.SwipeControl;
using SwipeControlOpenState = Microsoft.UI.Xaml.Controls.SwipeControlOpenState;
using SwipeItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.SwipeItemInvokedEventArgs;
using MaterialHelperTestApi = Microsoft.UI.Private.Media.MaterialHelperTestApi;
using SwipeTestHooks = Microsoft.UI.Private.Controls.SwipeTestHooks;
using MUXControlsTestHooks = Microsoft.UI.Private.Controls.MUXControlsTestHooks;
using MUXControlsTestHooksLoggingMessageEventArgs = Microsoft.UI.Private.Controls.MUXControlsTestHooksLoggingMessageEventArgs;

namespace MUXControlsTestApp
{
    public sealed partial class SwipeControlPage : TestPage
    {
        object asyncEventReportingLock = new object();
        List<string> lstAsyncEventMessage = new List<string>();
        List<string> fullLogs = new List<string>();
        FrameworkElement lastInteractedWithSwipeControlContentContainer;
        FrameworkElement lastInteractedWithSwipeControlContentRoot;
        SwipeItem pastSender;
        UIElement animatedSwipe;
        DispatcherTimer _dt;
        List<SwipeControl> swipeControls;
        List<long> swipeControlOpenStateChangedTokens = new List<long>();

        public SwipeControlPage()
        {
            // create command, and bind it to this object before initializing the components
            var command = new TestCommand(this);
            Resources.Add("command", command);

            this.InitializeComponent();
            Container.SizeChanged += ContainerSizeChangedHandler;
            SwipeTestHooks.LastInteractedWithSwipeControlChanged += SwipeTestHooks_LastInteractedWithSwipeControlChanged;
            MaterialHelperTestApiSetup();

            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.XamlUICommand"))
            {
                XamlUICommand uiCommand = new XamlUICommand
                {
                    Label = "UICommand Label",
                    IconSource = new SymbolIconSource { Symbol = Symbol.Setting }
                };

                uiCommand.ExecuteRequested += UICommand_ExecuteRequested;
                this.SwipeItemForUICommand.Command = uiCommand;
            }
            else
            {
                SwipeItemForUICommand.Text = "Non-UICommand Label";
                SwipeItemForUICommand.IconSource = Resources["FontIcon"] as IconSource;
            }

            if (chkLogSwipeControlMessages.IsChecked == true)
            {
                MUXControlsTestHooks.SetLoggingLevelForType("SwipeControl", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
                MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
            }

            swipeControls = new List<SwipeControl> { sc1, sc2, sc3, sc4, sc5, sc6, sc7, sc8, sc9, sc10, sc11 };
        }

        private void SwipeTestHooks_LastInteractedWithSwipeControlChanged(object sender, object args)
        {
            if (lastInteractedWithSwipeControlContentContainer != null)
            {
                lastInteractedWithSwipeControlContentContainer.SizeChanged -= LastInteractedWithSwipeControlSwipeContentElement_SizeChanged;
            }
            if(lastInteractedWithSwipeControlContentRoot != null)
            {
                lastInteractedWithSwipeControlContentRoot.SizeChanged -= LastInteractedWithSwipeControlSwipeContentElement_SizeChanged;
            }

            SwipeControl lastInteractedWithSwipeControl = SwipeTestHooks.GetLastInteractedWithSwipeControl();
            if (lastInteractedWithSwipeControl != null)
            {
                lastInteractedWithSwipeControlContentContainer = (FrameworkElement)FindVisualChildByName(SwipeTestHooks.GetLastInteractedWithSwipeControl(), "SwipeContentStackPanel");
                lastInteractedWithSwipeControlContentRoot = (FrameworkElement)FindVisualChildByName(SwipeTestHooks.GetLastInteractedWithSwipeControl(), "SwipeContentRoot");
            }

            if (lastInteractedWithSwipeControlContentContainer != null)
            {
                lastInteractedWithSwipeControlContentContainer.SizeChanged += LastInteractedWithSwipeControlSwipeContentElement_SizeChanged;
            }
            if (lastInteractedWithSwipeControlContentRoot != null)
            {
                lastInteractedWithSwipeControlContentRoot.SizeChanged += LastInteractedWithSwipeControlSwipeContentElement_SizeChanged;
            }

            PrintGridWidth();
        }

        private void LastInteractedWithSwipeControlSwipeContentElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PrintGridWidth();
        }

        private void UICommand_ExecuteRequested(XamlUICommand command, ExecuteRequestedEventArgs args)
        {
            ChangeText(command.Label);
        }

        // This is a helper that gives access to the textBlock. It's being called in the TestCommand class.
        public void ChangeText(string text)
        {
            textBlock.Text = text;
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
            peer.RaiseAutomationEvent(AutomationEvents.MenuClosed);
        }

        private void ContainerSizeChangedHandler(object sender, object e)
        {
            //var peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
            //peer.RaiseAutomationEvent(AutomationEvents.MenuOpened);

            PrintGridWidth();
        }

        private void MaterialHelperTestApiSetup()
        {
            MaterialHelperTestApi.IgnoreAreEffectsFast = true;
            MaterialHelperTestApi.SimulateDisabledByPolicy = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Unset all override flags to avoid impacting subsequent tests
            MaterialHelperTestApi.IgnoreAreEffectsFast = false;
            MaterialHelperTestApi.SimulateDisabledByPolicy = false;
            SwipeTestHooks.LastInteractedWithSwipeControlChanged -= SwipeTestHooks_LastInteractedWithSwipeControlChanged;
            base.OnNavigatedFrom(e);
        }

        private void OnResetClick(object sender, RoutedEventArgs args)
        {
            textBlock.Text = "";
            TextBox.Text = "";
        }

        private void PrintGridWidth()
        {
            String newText = "";

            if (lastInteractedWithSwipeControlContentRoot != null)
            {
                newText+= lastInteractedWithSwipeControlContentRoot.ActualWidth.ToString();
            }
            if (lastInteractedWithSwipeControlContentContainer != null)
            {
                newText += ", ";
                newText += lastInteractedWithSwipeControlContentContainer.ActualWidth.ToString();
            }

            TextBox.Text = newText;
        }

        private void SwipeItemInvoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (chkLogSwipeControlEvents.IsChecked == true)
            {
                AppendAsyncEventMessage("SwipeItemInvoked sender.Text=" + sender.Text + ", args.SwipeControl.Name=" + args.SwipeControl.Name);
            }

            // ensures that this method is invoked twice for only one swipe action.
            if (pastSender == sender)
            {
                textBlock.Text = "FailTest";
            }
            else
            {
                textBlock.Text = sender.Text;
            }
            pastSender = sender;

            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
            peer.RaiseAutomationEvent(AutomationEvents.MenuClosed);
        }

        private void ProgramaticallyCloseSwipeControl3(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            SwipeItemInvoked(sender, args);
            textBlock.Text = "sc3.Close()";
            sc3.Close();
        }

        private void ProgramaticallyCloseSwipeControl11(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            SwipeItemInvoked(sender, args);
            textBlock.Text = "sc11.Close()";
            sc11.Close();
        }

        private void ProgramaticallyCloseSwipeControl9(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            SwipeItemInvoked(sender, args);
            textBlock.Text = "sc9.Close()";
            sc9.Close();
        }

        private void ExecuteRemainOpenInvoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            textBlock.Text = sender.Text;
        }

        private void SwipeItemInvokedAndResizeGrid(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            SwipeItemInvoked(sender, args);

            if (sender.Text == "Scale Down")
            {
                SwipePanel.Width = 400;
            }
            else if (sender.Text == "Scale Up")
            {
                SwipePanel.Width = 700;
            }

            PrintGridWidth();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            _dt.Tick -= DispatcherTimer_Tick; // prevent leaks since the dispatcher holds a pointer to this
        }

        private void TestSwipeControl_Loaded(object sender, RoutedEventArgs e)
        {
            _dt = new DispatcherTimer();
            _dt.Interval = TimeSpan.FromSeconds(0.1);
            _dt.Start();
            _dt.Tick += DispatcherTimer_Tick;

            DependencyObject obj = FindVisualChildByName(SwipeControl4, "ContentRoot");
            UIElement ui = obj as UIElement;
            this.animatedSwipe = ui;

            SetupAnimatedValuesSpy();
            SpyAnimatedValues();
            foreach (var swipeControl in swipeControls)
            {
                swipeControlOpenStateChangedTokens.Add(swipeControl.RegisterPropertyChangedCallback(SwipeControl.OpenStateProperty, SwipeControl_PropertyChanged));
            }
            SwipeTestHooks.IdleStatusChanged += SwipeTestHooks_IdleStatusChanged;
        }

        private void TestSwipeControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var swipeControlCount = swipeControls.Count;
            if (swipeControlCount != swipeControlOpenStateChangedTokens.Count)
            {
                throw new InvalidOperationException("swipeControls.Count != swipeControlOpenStateChangedTokens.Count");
            }
            for (var i = 0; i < swipeControlCount; ++i)
            {
                swipeControls[i].UnregisterPropertyChangedCallback(SwipeControl.OpenStateProperty, swipeControlOpenStateChangedTokens[i]);
            }
            SwipeTestHooks.IdleStatusChanged -= SwipeTestHooks_IdleStatusChanged;
        }

        private void SwipeTestHooks_IdleStatusChanged(SwipeControl sender, object args)
        {
            if (chkLogSwipeControlEvents.IsChecked == true)
            {
                AppendAsyncEventMessage("SwipeTestHooks_IdleStatusChanged sender.Name=" + sender.Name);
            }

            if (sender.Name == this.sc1.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc1))
                {
                    this.SwipeItem1IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem1IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc2.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc2))
                {
                    this.SwipeItem2IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem2IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc3.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc3))
                {
                    this.SwipeItem3IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem3IdleCheckBox.IsChecked = false;
                }

                if (chkLogSwipeControlEvents.IsChecked == true)
                {
                    AppendAsyncEventMessage("SwipeTestHooks_IdleStatusChanged IsIdle=" + this.SwipeItem3IdleCheckBox.IsChecked.ToString());
                }

                Grid contentRootGrid = (Grid)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(sc3, 0), 0);
                SolidColorBrush solidColorBrush = (SolidColorBrush)contentRootGrid.Background;
                if (solidColorBrush != null)
                {
                    Swipe3BackgroundColorTextBlock.Text = solidColorBrush.Color.ToString();
                    if (chkLogSwipeControlEvents.IsChecked == true)
                    {
                        AppendAsyncEventMessage("SwipeTestHooks_IdleStatusChanged contentRootGrid.Background=" + solidColorBrush.Color.ToString());
                    }
                }
                else
                {
                    Swipe3BackgroundColorTextBlock.Text = "null";
                    if (chkLogSwipeControlEvents.IsChecked == true)
                    {
                        AppendAsyncEventMessage("SwipeTestHooks_IdleStatusChanged contentRootGrid.Background=null");
                    }
                }
            }
            if (sender.Name == this.sc4.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc4))
                {
                    this.SwipeItem4IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem4IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc5.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc5))
                {
                    this.SwipeItem5IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem5IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc6.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc6))
                {
                    this.SwipeItem6IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem6IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc7.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc7))
                {
                    this.SwipeItem7IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem7IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc8.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc8))
                {
                    this.SwipeItem8IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem8IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc9.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc9))
                {
                    this.SwipeItem9IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem9IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc10.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc10))
                {
                    this.SwipeItem10IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem10IdleCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc11.Name)
            {
                if (SwipeTestHooks.GetIsIdle(this.sc11))
                {
                    this.SwipeItem11IdleCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem11IdleCheckBox.IsChecked = false;
                }
            }
        }

        private void SwipeControl_PropertyChanged(DependencyObject obj, DependencyProperty dp)
        {
            SwipeControl sender = (SwipeControl) obj;
            if (chkLogSwipeControlEvents.IsChecked == true)
            {
                AppendAsyncEventMessage("SwipeControl_PropertyChanged sender.Name=" + sender.Name);
            }

            if (sender.Name == this.sc1.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem1OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem1OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc2.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem2OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem2OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc3.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem3OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem3OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc4.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem4OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem4OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc5.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem5OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem5OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc6.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem6OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem6OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc7.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem7OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem7OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc8.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem8OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem8OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc9.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem9OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem9OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc10.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem10OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem10OpenCheckBox.IsChecked = false;
                }
            }
            if (sender.Name == this.sc11.Name)
            {
                if (sender.OpenState == SwipeControlOpenState.Opened)
                {
                    this.SwipeItem11OpenCheckBox.IsChecked = true;
                }
                else
                {
                    this.SwipeItem11OpenCheckBox.IsChecked = false;
                }
            }
        }

        private void SetupAnimatedValuesSpy()
        {
            StopAnimatedValuesSpy();

            this.AnimatedValuesSpy = null;
            this.PositionAnimation = null;

            if (this.animatedSwipe != null)
            {
                Visual visualPPChild = ElementCompositionPreview.GetElementVisual(this.animatedSwipe);
                Compositor compositor = visualPPChild.Compositor;

                this.AnimatedValuesSpy = compositor.CreatePropertySet();
                AnimatedValuesSpy.InsertVector3("Offset", new Vector3());

                this.PositionAnimation = compositor.CreateExpressionAnimation("visual.Offset");
                this.PositionAnimation.SetReferenceParameter("visual", visualPPChild);

                CheckSpyingTicksRequirement();
                TickForValuesSpy();
            }
            else
            {
                ResetSpyOutput();
            }
        }

        private void DispatcherTimer_Tick(object sender, object e)
        {
            SpyAnimatedValues();
        }

        private void StartAnimatedValuesSpy()
        {
            if (this.AnimatedValuesSpy != null)
            {
                this.AnimatedValuesSpy.StartAnimation("Offset", this.PositionAnimation);
            }
        }

        private void StopAnimatedValuesSpy() // we fail before even reaching this
        {
            if (this.AnimatedValuesSpy != null)
            {
                this.AnimatedValuesSpy.StopAnimation("Offset");
            }
        }

        private void SpyAnimatedValues() // untestsed as well
        {
            if (this.AnimatedValuesSpy != null)
            {
                //StopAnimatedValuesSpy();

                Vector3 position;
                CompositionGetValueStatus status = this.AnimatedValuesSpy.TryGetVector3("Offset", out position);
                if (CompositionGetValueStatus.Succeeded == status)
                {
                    // add a position textblock in the file
                    this.PositionX.Text = position.X.ToString();
                    this.PositionY.Text = position.Y.ToString();
                }
                else
                {
                    this.PositionX.Text = this.PositionY.Text = "status=" + status.ToString();
                }

                StartAnimatedValuesSpy();
            }
        }

        private void ResetSpyOutput()
        {
            this.PositionX.Text = this.PositionY.Text = "0 - reset";
        }

        private void TickForValuesSpy()
        {
            this.UIThreadTicksForValuesSpy = 6;
            CheckSpyingTicksRequirement();
        }

        private void CheckSpyingTicksRequirement()
        {
            if (this.animatedSwipe != null &&
                (this.UIThreadTicksForValuesSpy > 0) && this.AnimatedValuesSpy != null)
            {
                if (!this.IsRenderingHooked)
                {
                    this.IsRenderingHooked = true;
                    Windows.UI.Xaml.Media.CompositionTarget.Rendering += CompositionTarget_Rendering;
                }
            }
            else
            {
                if (this.IsRenderingHooked)
                {
                    Windows.UI.Xaml.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
                    this.IsRenderingHooked = false;
                }
            }
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            if (this.UIThreadTicksForValuesSpy > 0)
            {
                this.UIThreadTicksForValuesSpy--;
            }
            CheckSpyingTicksRequirement();
            SpyAnimatedValues();
        }

        // Composition property spy stuff below:
        private CompositionPropertySet AnimatedValuesSpy
        {
            get;
            set;
        }

        private ExpressionAnimation PositionAnimation
        {
            get;
            set;
        }

        private bool IsRenderingHooked
        {
            get;
            set;
        }

        private uint UIThreadTicksForValuesSpy
        {
            get;
            set;
        }

        private void ChkLogSwipeControlMessages_Checked(object sender, RoutedEventArgs e)
        {
            //To turn on info and verbose logging for a particular SwipeControl instance:
            //MUXControlsTestHooks.SetLoggingLevelForInstance(sc1, isLoggingInfoLevel: true, isLoggingVerboseLevel: true);

            //To turn on info and verbose logging without any filter:
            //MUXControlsTestHooks.SetLoggingLevelForInstance(null, isLoggingInfoLevel: true, isLoggingVerboseLevel: true);

            //To turn on info and verbose logging for the SwipeControl type:
            MUXControlsTestHooks.SetLoggingLevelForType("SwipeControl", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);

            MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
        }

        private void ChkLogSwipeControlMessages_Unchecked(object sender, RoutedEventArgs e)
        {
            //To turn off info and verbose logging for a particular SwipeControl instance:
            //MUXControlsTestHooks.SetLoggingLevelForInstance(swipeControl, isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

            //To turn off info and verbose logging without any filter:
            //MUXControlsTestHooks.SetLoggingLevelForInstance(null, isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

            //To turn off info and verbose logging for the SwipeControl type:
            MUXControlsTestHooks.SetLoggingLevelForType("SwipeControl", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

            MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
        }

        private void MUXControlsTestHooks_LoggingMessage(object sender, MUXControlsTestHooksLoggingMessageEventArgs args)
        {
            // Cut off the terminating new line.
            string msg = args.Message.Substring(0, args.Message.Length - 1);
            string senderName = string.Empty;

            try
            {
                FrameworkElement fe = sender as FrameworkElement;

                if (fe != null)
                {
                    senderName = "s:" + fe.Name + ", ";
                }
            }
            catch
            {
                AppendAsyncEventMessage("Warning: Failure while accessing sender's Name");
            }

            if (args.IsVerboseLevel)
            {
                AppendAsyncEventMessage("Verbose: " + senderName + "m:" + msg);
            }
            else
            {
                AppendAsyncEventMessage("Info: " + senderName + "m:" + msg);
            }
        }

        private void AppendAsyncEventMessage(string asyncEventMessage)
        {
            lock (asyncEventReportingLock)
            {
                while (asyncEventMessage.Length > 0)
                {
                    string msgHead = asyncEventMessage;

                    if (asyncEventMessage.Length > 110)
                    {
                        int commaIndex = asyncEventMessage.IndexOf(',', 110);
                        if (commaIndex != -1)
                        {
                            msgHead = asyncEventMessage.Substring(0, commaIndex);
                            asyncEventMessage = asyncEventMessage.Substring(commaIndex + 1);
                        }
                        else
                        {
                            asyncEventMessage = string.Empty;
                        }
                    }
                    else
                    {
                        asyncEventMessage = string.Empty;
                    }

                    lstAsyncEventMessage.Add(msgHead);
                }

                var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, AppendAsyncEventMessage);
            }
        }

        private void AppendAsyncEventMessage()
        {
            lock (asyncEventReportingLock)
            {
                foreach (string asyncEventMessage in lstAsyncEventMessage)
                {
                    if (chkDisplayLogs.IsChecked == true)
                    {
                        lstSwipeControlEvents.Items.Add(asyncEventMessage);
                    }
                    fullLogs.Add(asyncEventMessage);
                }
                lstAsyncEventMessage.Clear();
            }
        }

        private void BtnClearSwipeControlEvents_Click(object sender, RoutedEventArgs e)
        {
            lstSwipeControlEvents.Items.Clear();
        }

        private void BtnGetFullLog_Click(object sender, RoutedEventArgs e)
        {
            foreach (string log in fullLogs)
            {
                cmbFullLog.Items.Add(log);
            }
        }

        private void BtnClearFullLog_Click(object sender, RoutedEventArgs e)
        {
            fullLogs.Clear();
            cmbFullLog.Items.Clear();
        }
    }
}