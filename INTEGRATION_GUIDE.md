# Integration Guide: Donations & Notifications (External Work/Production-Ready Setup)

This document outlines the step-by-step requirements, architecture, and code changes needed to transition the current development implementations of **Donations (Razorpay)** and **Notifications (RabbitMQ/Database)** into production-ready external integrations.

---

## 1. Donations Integration (Razorpay Checkout & Webhooks)

Currently, the donation system creates an order using the Razorpay SDK (with a mock fallback) and processes a webhook payload manually simulated by the developer dashboard. 

To make this work with the actual external Razorpay payment gateway:

### A. Razorpay Dashboard Configuration
1. **Sign Up / Log In**: Register on the [Razorpay Dashboard](https://dashboard.razorpay.com/).
2. **Retrieve API Keys**:
   - Switch to **Test Mode** (for development) or **Live Mode** (for production).
   - Go to **Account & Settings** > **API Keys** and generate a new key pair:
     - `Key ID` (e.g., `rzp_test_xxxxxx`)
     - `Key Secret` (e.g., `yyyyyyy`)
3. **Configure Webhook**:
   - Go to **Account & Settings** > **Webhooks**.
   - Add a new webhook URL: `https://<your-public-domain>/api/donations/webhook`
   - Set a **Webhook Secret** (this is critical for HMAC signature verification).
   - Under **Active Events**, check `payment.captured`.

### B. Backend Configuration (`appsettings.json`)
Update your configuration with the credentials retrieved from the Razorpay dashboard:

```json
{
  "Razorpay": {
    "KeyId": "rzp_test_yourActualKeyId",
    "KeySecret": "yourActualKeySecret",
    "WebhookSecret": "yourActualWebhookSecret"
  }
}
```

### C. Frontend Payment Gateway Checkout Integration
To allow alumni to make actual payments, you must embed the Razorpay Checkout script in `index.html` and launch the checkout interface when the user clicks the "Donate" button.

#### 1. Include Checkout Script in `<head>`
```html
<script src="https://checkout.razorpay.com/v1/checkout.js"></script>
```

#### 2. Update JavaScript Payment Handler in `index.html`
Instead of just calling the `/api/donations/create-order` endpoint and printing the response, instantiate the Razorpay checkout overlay. Once payment succeeds, Razorpay will submit a receipt which we can verify, or let the webhook capture it asynchronously:

```javascript
async function handleActualDonation(event) {
    event.preventDefault();
    const amount = parseFloat(document.getElementById('donate-amount').value);

    // Step 1: Request our ASP.NET Backend to create an order
    const response = await apiCall('/donations/create-order', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ amount: amount })
    });

    if (!response.ok) {
        alert("Failed to initiate donation order: " + response.body.message);
        return;
    }

    const orderData = response.body; // Contains OrderId and Amount

    // Step 2: Configure Razorpay Checkout Modal options
    const options = {
        "key": "rzp_test_yourActualKeyId", // Enter Key ID generated from Razorpay Dashboard
        "amount": orderData.amount * 100, // Amount in paise (e.g. ₹500 = 50000 paise)
        "currency": "INR",
        "name": "Alumni Association",
        "description": "Donation for Alumni Development",
        "order_id": orderData.orderId, // Order ID generated from backend
        "handler": function (response) {
            // This function runs on payment success in frontend
            alert("Payment successful! Payment ID: " + response.razorpay_payment_id);
            // Optionally call backend to verify/sync payment immediately:
            // apiCall('/donations/verify', { method: 'POST', body: JSON.stringify(response) });
            handleGetDonationHistory(); // Refresh history table
        },
        "prefill": {
            "name": currentUserEmail.split('@')[0],
            "email": currentUserEmail
        },
        "theme": {
            "color": "#3b82f6" // Theme color matching dashboard accent
        }
    };

    // Step 3: Open Razorpay modal
    const rzp1 = new Razorpay(options);
    rzp1.open();
}
```

### D. Webhook URL Testing on localhost (via Ngrok)
Razorpay's servers cannot reach `localhost`. To test the webhook locally:
1. Download and run [ngrok](https://ngrok.com/):
   ```bash
   ngrok http http://localhost:5286
   ```
2. Copy the secure forwarding URL (e.g., `https://a1b2-c3d4.ngrok-free.app`).
3. Set the Webhook URL in your Razorpay Dashboard to `https://a1b2-c3d4.ngrok-free.app/api/donations/webhook`.

### E. Option 2: Razorpay Hosted Payment Button Integration (No-Code Frontend)

Instead of generating orders programmatically on the backend and launching custom JavaScript checkout modules, you can use a pre-configured **Razorpay Payment Button**:

```html
<form>
  <script src="https://checkout.razorpay.com/v1/payment-button.js" data-payment_button_id="pl_T9Rmuv6EtpPdh2" async> </script> 
</form>
```

#### How it works:
1. When the page loads, the script renders an official Razorpay button.
2. When the user clicks it, it launches a checkout overlay hosted by Razorpay.
3. The payment is completed without hitting your `/api/donations/create-order` endpoint first.

#### Critical Webhook Adjustment:
Because the order is created on Razorpay's side directly, your database **will not have an existing order record** beforehand. The backend webhook (`payment.captured`) will fail during processing because it looks for a match on the `razorpayOrderId` and finds nothing.

To make the Payment Button work correctly, update `ProcessWebhookAsync` in `DonationService.cs` to handle dynamic creation:

```csharp
// If donation is not found by Razorpay Order ID, it came from a payment button
if (donation == null)
{
    // 1. Extract payment amount and user email from the webhook entity payload
    // 2. Lookup the User ID matching the email from the payload
    // 3. Create a new Donation record dynamically:
    donation = new Donation
    {
        UserId = matchedUserId, // Looked up via email
        Amount = amountInPaise / 100, // Convert paise to INR
        DonationDate = DateTime.UtcNow,
        razorpayOrderId = orderId,
        razorpayPaymentId = paymentId,
        CreatedAt = DateTime.UtcNow
    };
    _context.Donations.Add(donation);
}
```

---

## 2. Notifications Integration (WebSockets & Real-Time delivery)

Currently, the notification service stores notification logs inside the database. The background service consumes from a localhost RabbitMQ instance when jobs are posted, but notifications are *pull-based*—the user must click "Retrieve Feed" or reload the page to see them.

To make notification integrations work "like external work" (push-based, real-time, and via email):

### A. Real-Time WebSockets (SignalR Setup)
To send notifications to the user immediately when they occur without page reloads, integrate **ASP.NET Core SignalR**.

#### 1. Backend: Create a Hub class
Create `Hubs/NotificationHub.cs`:
```csharp
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace AlumniManagementApi.Hubs
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class NotificationHub : Hub
    {
        // Clients connect using WebSockets and are added to their respective User Groups
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }
    }
}
```

#### 2. Backend: Register SignalR in `Program.cs`
```csharp
builder.Services.AddSignalR();

// ... after builder.Build():
app.MapHub<NotificationHub>("/hubs/notifications");
```

#### 3. Backend: Trigger push notification in `JobPostedNotificationConsumer.cs`
Modify the RabbitMQ consumer to inject `IHubContext<NotificationHub>` and send message packets directly to connected users:

```csharp
using Microsoft.AspNetCore.SignalR;
using AlumniManagementApi.Hubs;

public class JobPostedNotificationConsumer : BackgroundService
{
    // Inject SignalR Hub context
    private readonly IHubContext<NotificationHub> _hubContext;
    
    // ... Inside ExecuteAsync when saving notification to DB:
    foreach (var user in alumniUsers)
    {
        var notification = new Notification
        {
            UserId = user.Id,
            Title = "New Job Posted",
            Type = Models.Type.NewJob,
            Message = $"A new job '{jobEvent.Title}' at '{jobEvent.Company}' has been posted.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        context.Notifications.Add(notification);

        // Send real-time SignalR push message to the connected user group
        await _hubContext.Clients.Group(user.Id.ToString()).SendAsync("ReceiveNotification", new {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            type = notification.Type.ToString(),
            createdAt = notification.CreatedAt,
            isRead = false
        });
    }
}
```

#### 4. Frontend Client Hub Connection (`index.html`)
Import the SignalR JS client and listen for notifications:
```html
<!-- Import SignalR CDN -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/6.0.1/signalr.min.js"></script>

<script>
    let connection = null;

    function startSignalRConnection() {
        if (!jwtToken) return;

        connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5286/hubs/notifications", {
                accessTokenFactory: () => jwtToken // Authenticate websocket handshake
            })
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveNotification", (notification) => {
            console.log("Real-time notification received: ", notification);
            
            // 1. Play audio ping or show toast message
            showNotificationToast(notification);

            // 2. Refresh notification feed if user is currently on the Notification panel
            const activePanel = document.querySelector('.panel.active');
            if (activePanel && activePanel.id === 'panel-notifications') {
                handleGetNotifications();
            }
        });

        connection.start()
            .then(() => console.log("SignalR WebSocket Connected!"))
            .catch(err => console.error("SignalR Connection Error: ", err));
    }

    function showNotificationToast(n) {
        // Build a sleek floating UI toast message card
        const toast = document.createElement('div');
        toast.style = "position: fixed; bottom: 20px; right: 20px; background: #1e293b; border-left: 4px solid #3b82f6; padding: 1rem; border-radius: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.5); z-index: 1000; animation: slideIn 0.3s ease;";
        toast.innerHTML = `<strong>${n.title}</strong><br><span style="font-size:0.8rem; color:#94a3b8">${n.message}</span>`;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 5000);
    }
    
    // Call startSignalRConnection() inside processToken(token) after login
</script>
```

---

### B. Email Notification Delivery (SendGrid / SMTP Integration)
For offline users, send email alerts using an external mail service.

#### 1. Define Email Interface
Create `Services/IEmailService.cs`:
```csharp
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string bodyHTML);
    }
}
```

#### 2. Implement utilizing SendGrid or standard SMTP
Create `Services/SendGridEmailService.cs`:
```csharp
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;

        public SendGridEmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SendGrid:ApiKey"] ?? string.Empty;
            _fromEmail = configuration["SendGrid:FromEmail"] ?? "no-reply@alumni-management.com";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyHTML)
        {
            if (string.IsNullOrEmpty(_apiKey)) return; // Skip if config is missing

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, "Alumni Platform Alerts");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, bodyHTML);
            await client.SendEmailAsync(msg);
        }
    }
}
```

#### 3. Invoke in Background Worker
Inject `IEmailService` inside `JobPostedNotificationConsumer.cs` and trigger:
```csharp
await _emailService.SendEmailAsync(
    user.Email,
    "New Job Opportunity: " + jobEvent.Title,
    $"<p>Hello,</p><p>A new job posting for <strong>{jobEvent.Title}</strong> at <strong>{jobEvent.Company}</strong> is now open. Log in to your alumni dashboard to apply.</p>"
);
```

---

### C. Production RabbitMQ Instance
Change localhost settings to a hosted RabbitMQ service (like CloudAMQP):
1. Register on [CloudAMQP](https://www.cloudamqp.com/).
2. Fetch the connection URI string.
3. Configure the AMQP credentials in `appsettings.json` and configure `ConnectionFactory` connection strings in `RabbitMQPublisher.cs` and `JobPostedNotificationConsumer.cs`.
