using DestallMaterials.Blazor.Components.Services.UI;
using DestallMaterials.Blazor.Services.UI;
using DestallMaterials.WheelProtection.DataStructures;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DestallMaterials.Blazor.Components
{
    public abstract partial class ViewComponent : ComponentBase, IDisposable
    {
        [Inject]
        protected IJSRuntime js { get; private set; }

        protected static IUiManipulator uiManipulator { get; private set; }

        protected DisposableList<IDisposable> Callbacks = new();

        protected DisposableCallback Subscribe(DisposableCallback callback)
        {
            Callbacks.Add(callback);
            return callback;
        }

        protected DisposableCallback[] Subscribe(DisposableCallback callback1, DisposableCallback callback2, params DisposableCallback[] other)
        {
            var result = new DisposableCallback[other.Length + 2];
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
