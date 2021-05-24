using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web;

public abstract class EntityBase
{
    internal virtual void OnSaving(ChangeAction changeAction) { }

    internal virtual void OnSaved() { }
}

internal class ChangeEntity
{
    public ChangeAction ChangeAction { get; set; }

    public EntityBase Entity { get; set; }
}