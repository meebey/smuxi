/*
 * $Id: Notebook.cs 143 2007-01-09 23:45:12Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/Notebook.cs $
 * $Rev: 143 $
 * $Author: meebey $
 * $Date: 2007-01-10 00:45:12 +0100 (Wed, 10 Jan 2007) $
 *
 */

using System;
using Smuxi;

namespace Smuxi.Frontend.Gnome
{
    public abstract class ViewManager
    {
        protected abstract void CreateView();
    }
}
