using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class ComingSoonController : Controller
{
    // GET: ComingSoon
    public IActionResult Index(string? feature = null)
    {
        var model = new ComingSoonViewModel
        {
            FeatureName = feature ?? "Feature",
            Icon = "fa-rocket",
            Color = "success"
        };

        // Set specific details based on feature
        switch (feature?.ToLower())
        {
            case "giftcards":
                model.FeatureName = "Gift Cards";
                model.Icon = "fa-gift";
                model.Color = "warning";
                model.Description = "Create, sell, and redeem gift cards for your customers.";
                model.Features = new List<string>
                {
                    "Generate unique gift card numbers",
                    "Sell gift cards to customers",
                    "Redeem gift cards at checkout",
                    "Check gift card balances",
                    "View gift card transaction history"
                };
                break;

            case "loyalty":
                model.FeatureName = "Loyalty Program";
                model.Icon = "fa-star";
                model.Color = "info";
                model.Description = "Build customer loyalty with points, tiers, and rewards.";
                model.Features = new List<string>
                {
                    "Earn points per Rand spent",
                    "Tier system (Bronze, Silver, Gold, Platinum)",
                    "Redeem points for discounts",
                    "Birthday and anniversary bonuses",
                    "Loyalty analytics dashboard"
                };
                break;

            case "mobile":
                model.FeatureName = "Mobile App";
                model.Icon = "fa-mobile-alt";
                model.Color = "primary";
                model.Description = "Take your POS on the go with our mobile app.";
                model.Features = new List<string>
                {
                    "Process sales on mobile devices",
                    "Real-time inventory management",
                    "Customer lookup on the go",
                    "Offline mode support",
                    "Sales analytics and reports"
                };
                break;

            case "ecommerce":
                model.FeatureName = "E-Commerce Integration";
                model.Icon = "fa-shopping-bag";
                model.Color = "danger";
                model.Description = "Connect your online store with your POS system.";
                model.Features = new List<string>
                {
                    "Sync products with online store",
                    "Manage inventory across channels",
                    "Customer sync between systems",
                    "Order management dashboard",
                    "Multi-channel reporting"
                };
                break;

            case "accounting":
                model.FeatureName = "Accounting Integration";
                model.Icon = "fa-calculator";
                model.Color = "secondary";
                model.Description = "Connect with popular accounting software.";
                model.Features = new List<string>
                {
                    "QuickBooks integration",
                    "Xero integration",
                    "Sage integration",
                    "Automated invoice sync",
                    "Tax reporting and compliance"
                };
                break;

            default:
                model.FeatureName = feature ?? "New Feature";
                model.Icon = "fa-rocket";
                model.Color = "success";
                model.Description = "We're working on something amazing!";
                model.Features = new List<string>
                {
                    "Stay tuned for updates",
                    "We'll notify you when it's ready"
                };
                break;
        }

        model.EstimatedRelease = DateTime.Now.AddMonths(2);

        return View(model);
    }
}