using EverModern.Blazor.Components.Services.UI;
using EverModern.WheelProtection.DataStructures;
using EverModern.WheelProtection.DataStructures.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EverModern.Blazor.Components.Common
{
    public abstract partial class ViewComponent : ComponentBase, IDisposable
    {
        [Inject]
        protected IJSRuntime js { get; private set; }

        protected static IUiManipulator uiManipulator { get; private set; }

        protected DisposableList<IDisposable> Callbacks = [];

        protected Subscription Subscribe(Subscription callback)
        {
            Callbacks.Add(callback);
            return callback;
        }

        protected Subscription[] Subscribe(Subscription callback1, Subscription callback2, params Subscription[] other)
        {
            var result = new Subscription[other.Length + 2];
            for (int i = 2; i < result.Length; i++)
            {
                result[i] = other[i - 2];
            }
            result[0] = callback1;
            result[1] = callback2;
            Callbacks.AddRange(result);
            return result;
        }

        public virtual void Dispose()
        {
            Callbacks.Dispose();
        }

        protected sealed override async Task OnInitializedAsync()
        {
            if (uiManipulator == null)
            {
                uiManipulator = new JsUiManipulator(js);
            }

            await base.OnInitializedAsync();
            await _onInitializedAsync();
        }
        protected virtual async Task _onInitializedAsync()
        {
        }
    }
}
