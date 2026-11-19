using Company.Application.Share.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Company.Application.Share.Event
{
    public class LoginSuccessEvent:PubSubEvent<CurrentUser>
    {
    }
}
