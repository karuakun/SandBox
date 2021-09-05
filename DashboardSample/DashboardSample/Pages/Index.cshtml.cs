using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.QuickSight;
using Amazon.QuickSight.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace DashboardSample.Pages
{
    public class IndexModel : PageModel
    {
        public SelectList DashboardList { get; set; }
        public string SelectedDashboard { get; set; }
        public string EmbedDashboardUrl { get; set; }


        private readonly ILogger<IndexModel> _logger;
        private readonly IAmazonSecurityTokenService _amazonSecurityTokenService;
        private readonly IAmazonQuickSight _amazonQuickSight;
        

        public IndexModel(
            ILogger<IndexModel> logger,
            IAmazonSecurityTokenService amazonSecurityTokenService,
            IAmazonQuickSight amazonQuickSight
            )
        {
            _logger = logger;
            _amazonSecurityTokenService = amazonSecurityTokenService;
            _amazonQuickSight = amazonQuickSight;
        }

        public async Task OnGet()
        {
            var account = await _amazonSecurityTokenService.GetCallerIdentityAsync(new GetCallerIdentityRequest());
            await LoadDashBoardsAsync(account.Account);
        }
        
        public async Task OnPostViewDashboardForAdmin(string dashboard, string @namespace)
        {
            var account = await _amazonSecurityTokenService.GetCallerIdentityAsync(new GetCallerIdentityRequest());
            await LoadDashBoardsAsync(account.Account);

            SelectedDashboard = dashboard;
            EmbedDashboardUrl = await GenerateEmbedUrlForAnonymousUserAsync(account.Account ,dashboard, "user,admin");
        }
        public async Task OnPostViewDashboardForUser(string dashboard, string @namespace)
        {
            var account = await _amazonSecurityTokenService.GetCallerIdentityAsync(new GetCallerIdentityRequest());
            await LoadDashBoardsAsync(account.Account);

            SelectedDashboard = dashboard;
            EmbedDashboardUrl = await GenerateEmbedUrlForAnonymousUserAsync(account.Account, dashboard, "user");
        }

        public async Task OnPostViewDashboardForTest1User(string dashboard)
        {
            const string userName = "test1@example.com";
            var account = await _amazonSecurityTokenService.GetCallerIdentityAsync(new GetCallerIdentityRequest());
            await LoadDashBoardsAsync(account.Account);

            // ユーザーを取得（いなかったら登録）してグループに追加
            var quickSightUserArn = await GetUserArnAsync(account.Account, "default", userName);
            if (string.IsNullOrEmpty(quickSightUserArn))
            {
                quickSightUserArn = await RegisterUserAsync(account.Account, "default", userName, UserRole.READER);
            }
            await _amazonQuickSight.CreateGroupMembershipAsync(
                new CreateGroupMembershipRequest
                {
                    AwsAccountId = account.Account,
                    GroupName = "SampleDashboard",
                    Namespace = "default",
                    MemberName = userName
                });

            // ダッシュボードを表示
            var response = await _amazonQuickSight.GenerateEmbedUrlForRegisteredUserAsync(new GenerateEmbedUrlForRegisteredUserRequest
            {
                AwsAccountId = account.Account,
                ExperienceConfiguration = new RegisteredUserEmbeddingExperienceConfiguration
                {
                    Dashboard = new RegisteredUserDashboardEmbeddingConfiguration
                    {
                        InitialDashboardId = dashboard,
                    }
                },
                UserArn = quickSightUserArn,
                SessionLifetimeInMinutes = 15,
            });
            EmbedDashboardUrl = response.EmbedUrl;
        }
        public async Task LoadDashBoardsAsync(string awsAccountId)
        {
            // ページングしてないので注意
            var response = await _amazonQuickSight.ListDashboardsAsync(new ListDashboardsRequest
            {
                AwsAccountId = awsAccountId
            });
            DashboardList = new SelectList(
                response.DashboardSummaryList,
                nameof(DashboardSummary.DashboardId),
                nameof(DashboardSummary.Name));
        }

        public async Task<string> GenerateEmbedUrlForAnonymousUserAsync(string awsAccountId, string dashboardId, string role)
        {
            var dashboard = await _amazonQuickSight.DescribeDashboardAsync(new DescribeDashboardRequest
            {
                AwsAccountId = awsAccountId,
                DashboardId = dashboardId
            });

            var response = await _amazonQuickSight.GenerateEmbedUrlForAnonymousUserAsync(new GenerateEmbedUrlForAnonymousUserRequest
            {
                AwsAccountId = awsAccountId,
                Namespace = "default",
                AuthorizedResourceArns = new List<string>
                {
                    dashboard.Dashboard.Arn
                },
                ExperienceConfiguration = new AnonymousUserEmbeddingExperienceConfiguration
                {
                    Dashboard = new AnonymousUserDashboardEmbeddingConfiguration
                    {
                        InitialDashboardId = dashboardId,
                    }
                },
                SessionTags = new List<SessionTag>
                {
                    new() {Key = "tag_role", Value = role}
                },
                SessionLifetimeInMinutes = 60,
            });
            return response.EmbedUrl;
        }

        public async Task<string> GetUserArnAsync(string awsAccountId, string @namespace, string userName)
        {
            try
            {
                var user = await _amazonQuickSight.DescribeUserAsync(new DescribeUserRequest
                {
                    AwsAccountId = awsAccountId,
                    Namespace = @namespace,
                    UserName = userName
                });
                return user.User.Arn;
            }
            catch (ResourceNotFoundException)
            {
                return null;
            }
        }
        public async Task<string> RegisterUserAsync(string awsAccountId, string @namespace, string userName, UserRole userRole)
        {
            var registerUserResponse = await _amazonQuickSight.RegisterUserAsync(new RegisterUserRequest
            {
                AwsAccountId = awsAccountId,
                Namespace = @namespace,
                IdentityType = IdentityType.QUICKSIGHT,
                Email = userName,
                UserName = userName,
                UserRole = userRole
            });
            return registerUserResponse.User.Arn;
        }
    }
}
