using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AIUsageGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_usage_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ToolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PromptPreview = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    InputTokenCount = table.Column<int>(type: "integer", nullable: true),
                    OutputTokenCount = table.Column<int>(type: "integer", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetId = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsSecurityRelevant = table.Column<bool>(type: "boolean", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ClientIpAddressHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "background_job_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledForUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ProcessedItemCount = table.Column<int>(type: "integer", nullable: false),
                    FailureSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_background_job_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UrgentAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DigestEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DigestCadence = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    RecipientSelectionMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SelectedRecipientUserIds = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TriggerFingerprint = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SummaryBody = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CoveredPeriodStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CoveredPeriodEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByJobRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plan_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RetiredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSignInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedSignInCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastFailedSignInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_delivery_outcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DeliveryStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_delivery_outcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_delivery_outcomes_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_limit_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IncludedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WarningThresholdQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    HardLimitQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    LimitBehavior = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_limit_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_limit_rules_plan_definitions_PlanDefinitionId",
                        column: x => x.PlanDefinitionId,
                        principalTable: "plan_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_claims_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_claims_user_accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "user_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_user_logins_user_accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "user_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_user_accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "user_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_user_tokens_user_accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "user_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_evaluation_outcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedRuleVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MatchedRuleCount = table.Column<int>(type: "integer", nullable: false),
                    EvaluationResult = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SkippedReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EvidenceSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_evaluation_outcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_risk_evaluation_outcomes_ai_usage_events_EventId",
                        column: x => x.EventId,
                        principalTable: "ai_usage_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_risk_evaluation_outcomes_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workspace_memberships_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_plan_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFromCycleStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveToCycleStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_plan_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workspace_plan_assignments_plan_definitions_PlanDefinitionId",
                        column: x => x.PlanDefinitionId,
                        principalTable: "plan_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workspace_plan_assignments_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_risk_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedTools = table.Column<string>(type: "text", nullable: false),
                    PerEventEstimatedCostThreshold = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DailyEstimatedCostThreshold = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_risk_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workspace_risk_policies_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationOutcomeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EvidencePreview = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_risk_findings_ai_usage_events_EventId",
                        column: x => x.EventId,
                        principalTable: "ai_usage_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_risk_findings_risk_evaluation_outcomes_EvaluationOutcomeId",
                        column: x => x.EvaluationOutcomeId,
                        principalTable: "risk_evaluation_outcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_risk_findings_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_cycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CycleEndExclusiveUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlanAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCalculatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AdjustmentCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_cycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usage_cycles_workspace_plan_assignments_PlanAssignmentId",
                        column: x => x.PlanAssignmentId,
                        principalTable: "workspace_plan_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usage_cycles_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cycle_adjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AdjustmentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DeltaQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedByJobRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cycle_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cycle_adjustments_background_job_runs_AppliedByJobRunId",
                        column: x => x.AppliedByJobRunId,
                        principalTable: "background_job_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cycle_adjustments_usage_cycles_UsageCycleId",
                        column: x => x.UsageCycleId,
                        principalTable: "usage_cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "limit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TriggeredBySourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TriggeredBySourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ThresholdQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_limit_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_limit_events_usage_cycles_UsageCycleId",
                        column: x => x.UsageCycleId,
                        principalTable: "usage_cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_limit_events_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_cycle_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IncludedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WarningThresholdQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    HardLimitQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    CurrentQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OverageQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LimitBehavior = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastTransitionAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_cycle_metrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usage_cycle_metrics_usage_cycles_UsageCycleId",
                        column: x => x.UsageCycleId,
                        principalTable: "usage_cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_ActorUserId_OccurredAt",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "ActorUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_EventType_OccurredAt",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_IdempotencyKey",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_OccurredAt",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_OccurredAt_EstimatedCost",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "OccurredAt", "EstimatedCost" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_ToolName_OccurredAt",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "ToolName", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_ActorUserId",
                table: "audit_records",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_WorkspaceId_ActionType_OccurredAt",
                table: "audit_records",
                columns: new[] { "WorkspaceId", "ActionType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_WorkspaceId_ActorUserId_OccurredAt",
                table: "audit_records",
                columns: new[] { "WorkspaceId", "ActorUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_WorkspaceId_IsSecurityRelevant_OccurredAt",
                table: "audit_records",
                columns: new[] { "WorkspaceId", "IsSecurityRelevant", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_WorkspaceId_OccurredAt",
                table: "audit_records",
                columns: new[] { "WorkspaceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_WorkspaceId_Result_OccurredAt",
                table: "audit_records",
                columns: new[] { "WorkspaceId", "Result", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_background_job_runs_JobType_ScheduledForUtc",
                table: "background_job_runs",
                columns: new[] { "JobType", "ScheduledForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_background_job_runs_WorkspaceId_JobType_StartedAtUtc",
                table: "background_job_runs",
                columns: new[] { "WorkspaceId", "JobType", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cycle_adjustments_AppliedByJobRunId",
                table: "cycle_adjustments",
                column: "AppliedByJobRunId");

            migrationBuilder.CreateIndex(
                name: "IX_cycle_adjustments_UsageCycleId_RecordedAtUtc",
                table: "cycle_adjustments",
                columns: new[] { "UsageCycleId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_limit_events_UsageCycleId",
                table: "limit_events",
                column: "UsageCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_limit_events_WorkspaceId_UsageCycleId_Dimension_EventType_T~",
                table: "limit_events",
                columns: new[] { "WorkspaceId", "UsageCycleId", "Dimension", "EventType", "TriggeredBySourceType", "TriggeredBySourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_limit_events_WorkspaceId_UsageCycleId_Dimension_OccurredAtU~",
                table: "limit_events",
                columns: new[] { "WorkspaceId", "UsageCycleId", "Dimension", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_outcomes_DeliveryStatus_NextAttemptAt~",
                table: "notification_delivery_outcomes",
                columns: new[] { "DeliveryStatus", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_outcomes_NotificationId_DeliveryStatus",
                table: "notification_delivery_outcomes",
                columns: new[] { "NotificationId", "DeliveryStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_outcomes_NotificationId_RecipientUser~",
                table: "notification_delivery_outcomes",
                columns: new[] { "NotificationId", "RecipientUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_WorkspaceId",
                table: "notification_preferences",
                column: "WorkspaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_WorkspaceId_CreatedAtUtc",
                table: "notifications",
                columns: new[] { "WorkspaceId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_WorkspaceId_NotificationType_CreatedAtUtc",
                table: "notifications",
                columns: new[] { "WorkspaceId", "NotificationType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_WorkspaceId_NotificationType_TriggerFingerpri~",
                table: "notifications",
                columns: new[] { "WorkspaceId", "NotificationType", "TriggerFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_WorkspaceId_Status_CreatedAtUtc",
                table: "notifications",
                columns: new[] { "WorkspaceId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_plan_definitions_IsActive_IsDefault",
                table: "plan_definitions",
                columns: new[] { "IsActive", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_plan_definitions_PlanCode",
                table: "plan_definitions",
                column: "PlanCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_limit_rules_PlanDefinitionId_Dimension",
                table: "plan_limit_rules",
                columns: new[] { "PlanDefinitionId", "Dimension" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_risk_evaluation_outcomes_EventId",
                table: "risk_evaluation_outcomes",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_risk_evaluation_outcomes_WorkspaceId_EvaluatedAt",
                table: "risk_evaluation_outcomes",
                columns: new[] { "WorkspaceId", "EvaluatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_risk_findings_EvaluationOutcomeId",
                table: "risk_findings",
                column: "EvaluationOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_risk_findings_EventId",
                table: "risk_findings",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_risk_findings_WorkspaceId_ActorUserId_DetectedAt",
                table: "risk_findings",
                columns: new[] { "WorkspaceId", "ActorUserId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_risk_findings_WorkspaceId_DetectedAt",
                table: "risk_findings",
                columns: new[] { "WorkspaceId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_risk_findings_WorkspaceId_EventId_RuleType",
                table: "risk_findings",
                columns: new[] { "WorkspaceId", "EventId", "RuleType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_risk_findings_WorkspaceId_RuleType_DetectedAt",
                table: "risk_findings",
                columns: new[] { "WorkspaceId", "RuleType", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_risk_findings_WorkspaceId_Severity_DetectedAt",
                table: "risk_findings",
                columns: new[] { "WorkspaceId", "Severity", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_risk_findings_WorkspaceId_ToolName_DetectedAt",
                table: "risk_findings",
                columns: new[] { "WorkspaceId", "ToolName", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_role_claims_RoleId",
                table: "role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usage_cycle_metrics_UsageCycleId_Dimension",
                table: "usage_cycle_metrics",
                columns: new[] { "UsageCycleId", "Dimension" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usage_cycles_PlanAssignmentId",
                table: "usage_cycles",
                column: "PlanAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_cycles_WorkspaceId_CycleStartUtc",
                table: "usage_cycles",
                columns: new[] { "WorkspaceId", "CycleStartUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usage_cycles_WorkspaceId_Status_CycleStartUtc",
                table: "usage_cycles",
                columns: new[] { "WorkspaceId", "Status", "CycleStartUtc" });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "user_accounts",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_LockedUntilUtc",
                table: "user_accounts",
                column: "LockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "user_accounts",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_claims_UserId",
                table: "user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_UserId",
                table: "user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_memberships_WorkspaceId_UserId",
                table: "workspace_memberships",
                columns: new[] { "WorkspaceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_plan_assignments_PlanDefinitionId",
                table: "workspace_plan_assignments",
                column: "PlanDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_plan_assignments_WorkspaceId_EffectiveFromCycleSt~",
                table: "workspace_plan_assignments",
                columns: new[] { "WorkspaceId", "EffectiveFromCycleStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_plan_assignments_WorkspaceId_EffectiveToCycleStar~",
                table: "workspace_plan_assignments",
                columns: new[] { "WorkspaceId", "EffectiveToCycleStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_risk_policies_WorkspaceId",
                table: "workspace_risk_policies",
                column: "WorkspaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_Slug",
                table: "workspaces",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_records");

            migrationBuilder.DropTable(
                name: "cycle_adjustments");

            migrationBuilder.DropTable(
                name: "limit_events");

            migrationBuilder.DropTable(
                name: "notification_delivery_outcomes");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "plan_limit_rules");

            migrationBuilder.DropTable(
                name: "risk_findings");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "usage_cycle_metrics");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "workspace_memberships");

            migrationBuilder.DropTable(
                name: "workspace_risk_policies");

            migrationBuilder.DropTable(
                name: "background_job_runs");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "risk_evaluation_outcomes");

            migrationBuilder.DropTable(
                name: "usage_cycles");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "ai_usage_events");

            migrationBuilder.DropTable(
                name: "workspace_plan_assignments");

            migrationBuilder.DropTable(
                name: "plan_definitions");

            migrationBuilder.DropTable(
                name: "workspaces");
        }
    }
}
