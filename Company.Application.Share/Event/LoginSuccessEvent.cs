using Company.Applications.Share.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Company.Applications.Share.Event
{
    public class LoginSuccessEvent:PubSubEvent<CurrentUser>
    {
    }
}
