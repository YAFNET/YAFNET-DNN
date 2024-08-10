/* Yet Another Forum.NET
 * Copyright (C) 2006-2012 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

namespace YAF.DotNetNuke.Components;

using System.Web;

using Autofac;

using YAF.Core;
using YAF.Types.Attributes;
using YAF.Types.EventProxies;
using YAF.Types.Interfaces;
using YAF.Types.Interfaces.Events;

/// <summary>
/// Lifecycle module used to throw events around...
/// </summary>
public class YafTaskModule : IHttpModule, IHaveServiceLocator
{
    /// <summary>
    ///   The _app instance.
    /// </summary>
    protected HttpApplication AppInstance;

    /// <summary>
    ///   The _module initialized.
    /// </summary>
    protected bool ModuleInitialized;

    /// <summary>
    ///   Gets or sets the logger associated with the object.
    /// </summary>
    [Inject]
    public ILoggerService Logger { get; set; }

    [Inject]
    public IServiceLocator ServiceLocator { get; set; }

    /// <summary>
    /// Bootstrapping fun
    /// </summary>
    /// <param name="context">
    /// The http application.
    /// </param>
    public void Init(HttpApplication context)
    {
        if (this.ModuleInitialized)
        {
            return;
        }

        // create a lock so no other instance can affect the static variable
        lock (this)
        {
            if (!this.ModuleInitialized)
            {
                this.AppInstance = context;

                // set the httpApplication as early as possible...
                GlobalContainer.Container.Resolve<CurrentHttpApplicationStateBaseProvider>().Instance =
                    new HttpApplicationStateWrapper(context.Application);

                GlobalContainer.Container.Resolve<IInjectServices>().Inject(this);

                this.ModuleInitialized = true;
            }
        }

        // app init notification...
        this.Get<IRaiseEvent>().RaiseIssolated(new HttpApplicationInitEvent(this.AppInstance), null);
    }

    /// <summary>
    /// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule"/>.
    /// </summary>
    void IHttpModule.Dispose()
    {
    }
}