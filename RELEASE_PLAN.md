# Forge Release Plan

## Executive Summary

**Product:** Forge - AI Agent Dashboard for orchestrating and monitoring AI coding agents powered by Claude Code CLI

**Market Opportunity:** .NET developers are underserved by existing AI tooling, which predominantly targets Node.js and Python ecosystems. Forge provides a .NET-first solution with enterprise-grade features.

**Business Model:** Freemium with per-seat subscription

**Target Launch:** MVP in 1-2 months

---

## Product Definition

### Core Value Proposition

Forge transforms the raw Claude Code CLI into an enterprise-ready development workflow by adding:

| Capability | Raw CLI | Forge |
|------------|---------|-------|
| Task orchestration | Manual | Automated pipeline |
| Human oversight | None | Configurable approval gates |
| Parallel execution | Manual worktrees | Automatic subtask isolation |
| Error recovery | Manual | Automatic rollback with audit trail |
| Progress visibility | Terminal output | Real-time dashboard |
| Team collaboration | None | Shared task queue |

### Key Differentiators

1. **Pipeline Orchestration** - Structured workflow (Backlog → Split → Research → Planning → Implementing → Simplifying → Verifying → Reviewing → PR Ready → Done)
2. **Human-in-the-Loop Gates** - Confidence-based escalation and mandatory approval checkpoints
3. **Subtask Isolation** - Git worktree-based parallel execution with automatic cleanup
4. **Rollback Capability** - Granular undo with full artifact preservation
5. **Framework Detection** - Automatic variant selection (Angular, .NET, etc.)

### Agent Support

- **MVP:** Claude Code CLI (primary supported agent)
- **Future:** Extensible architecture for additional agents (OpenAI Codex, local models)

---

## Pricing Strategy

### Tier Structure

| Tier | Price | Task Limits | Projects | Key Features |
|------|-------|-------------|----------|--------------|
| **Free** | $0 | 5 tasks/day | 1 | Core pipeline, local execution, community support |
| **Pro** | $7/month | Unlimited | 5 | Full pipeline, priority scheduling, email support |
| **Team** | $15/seat/month | Unlimited | Unlimited | Shared dashboard, audit logs, API access, usage analytics |
| **Enterprise** | Custom | Unlimited | Unlimited | SSO, on-premises option, SLA, dedicated support, compliance |

### Pricing Rationale

- **$7 Pro tier** sits in the sweet spot ($5-9 range) for individual developers
- Volume-focused pricing to maximize adoption
- Clear upgrade path as teams grow
- Enterprise tier captures high-value customers without limiting growth

### Free Tier Strategy

The free tier is designed to:
- Lower barrier to entry for .NET developers
- Demonstrate value before purchase decision
- Create habit formation with daily usage
- Generate word-of-mouth through community sharing

---

## Distribution Strategy

### Primary Distribution

```bash
# Install globally via .NET CLI
dotnet tool install -g forge

# Authenticate (opens browser OAuth)
forge login

# Initialize in project
forge init

# Start dashboard
forge start
```

### License Validation Flow

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   forge     │────▶│  License     │────▶│   Tier      │
│   start     │     │  Server      │     │   Features  │
└─────────────┘     └──────────────┘     └─────────────┘
                           │
                    ┌──────┴──────┐
                    │  Periodic   │
                    │  Heartbeat  │
                    └─────────────┘
```

- **Startup:** License check on first run and daily thereafter
- **Heartbeat:** Periodic validation (every 4 hours) to sync tier changes
- **Offline Grace:** 72-hour offline operation before re-authentication required
- **Degradation:** Graceful fallback to free tier limits on validation failure

### Authentication Providers

1. GitHub OAuth (primary - most .NET developers have accounts)
2. Microsoft Account (secondary - enterprise users)

---

## MVP Technical Requirements (Month 1-2)

### Must Have (P0)

- [ ] **dotnet tool packaging**
  - Self-contained executable
  - Cross-platform (Windows, macOS, Linux)
  - Automatic update notifications

- [ ] **License server**
  - Simple REST API for validation
  - User registration and tier storage
  - Rate limiting and abuse prevention

- [ ] **OAuth login flow**
  - GitHub OAuth integration
  - Microsoft Account integration
  - Local token storage (secure)

- [ ] **Tier enforcement**
  - Task counting per day
  - Project limit enforcement
  - Feature gating by tier

- [ ] **Basic telemetry (opt-in)**
  - Anonymous usage statistics
  - Error reporting
  - Feature usage tracking

### Nice to Have (P1)

- [ ] Usage dashboard for users (view remaining tasks, upgrade prompts)
- [ ] Stripe integration (automated billing)
- [ ] Email notifications (task completion, gate requests)
- [ ] CLI auto-update mechanism

### Deferred (P2)

- [ ] Team management UI
- [ ] API key generation
- [ ] Webhook integrations

---

## Feature Roadmap

### Phase 1: MVP (Month 1-2)

**Goal:** Package current Forge features as a commercial product

| Feature | Status | Notes |
|---------|--------|-------|
| Current pipeline features | Ready | Existing codebase |
| dotnet tool packaging | TODO | Self-contained publish |
| Free tier with limits | TODO | Task/project limits |
| Pro tier unlock | TODO | Payment integration |
| License validation | TODO | Azure Functions backend |
| GitHub OAuth | TODO | Primary auth method |

### Phase 2: Team Features (Month 3-4)

**Goal:** Enable team collaboration and enterprise evaluation

| Feature | Priority | Notes |
|---------|----------|-------|
| Team dashboard | High | Shared task visibility |
| User management | High | Invite/remove team members |
| Usage analytics | Medium | Per-user and team metrics |
| Audit logs | Medium | Action history for compliance |
| Azure DevOps integration | Medium | Work item sync |
| GitHub Issues integration | Low | Bi-directional sync |

### Phase 3: Enterprise (Month 6+)

**Goal:** Enterprise-ready deployment options

| Feature | Priority | Notes |
|---------|----------|-------|
| SSO integration | High | Azure AD, Okta, SAML |
| On-premises deployment | High | Docker/Kubernetes |
| Multi-agent support | Medium | OpenAI, local models |
| Compliance features | Medium | SOC2, GDPR support |
| Custom agent configs | Low | Per-org agent variants |
| White-labeling | Low | Custom branding |

---

## Go-to-Market Strategy

### Pre-Launch (Weeks 1-2)

**Build anticipation and validate demand**

- [ ] Landing page with email waitlist
  - Highlight .NET-first positioning
  - Show pipeline visualization
  - Collect email for launch notification

- [ ] Social presence
  - Twitter/X: @ForgeDevTools (or similar)
  - Regular posts about AI + .NET development
  - Engage with .NET community

- [ ] Community engagement
  - r/dotnet posts about AI tooling challenges
  - r/csharp engagement
  - .NET Discord servers

### Launch (Week of Release)

**Maximize visibility across developer communities**

- [ ] **Product Hunt launch**
  - Schedule for Tuesday/Wednesday
  - Prepare assets (screenshots, video demo)
  - Rally early users for upvotes

- [ ] **Hacker News Show HN**
  - Technical deep-dive post
  - Highlight novel architecture decisions
  - Be available for Q&A

- [ ] **Blog post: "Why .NET Developers Deserve Better AI Tooling"**
  - Publish on dev.to, Medium, personal blog
  - Cross-post to r/dotnet
  - Share on Twitter/LinkedIn

- [ ] **.NET community Discord/Slack**
  - Announce in relevant channels
  - Offer extended trial for early feedback

### Post-Launch (Ongoing)

**Sustained growth and community building**

- [ ] **Influencer outreach**
  - Nick Chapsas (YouTube)
  - Tim Corey (YouTube)
  - Scott Hanselman (blog/podcast)
  - David Fowler (Twitter)

- [ ] **Conference presence**
  - .NET Conf lightning talk submission
  - Local .NET meetup presentations
  - NDC/DevIntersection talk proposals

- [ ] **Content marketing**
  - Weekly tips on Twitter
  - Monthly blog posts
  - Case studies from early adopters
  - Video tutorials

---

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation Strategy |
|------|------------|--------|---------------------|
| **Anthropic ships native orchestrator** | Medium | High | Build moat through enterprise features (human gates, audit trails, compliance); accelerate multi-agent roadmap |
| **Claude CLI breaking changes** | Medium | Medium | Pin CLI versions; comprehensive integration tests; rapid patching process; maintain compatibility layer |
| **Low free→paid conversion** | High | Medium | Generous free tier for adoption; clear value demonstration; in-app upgrade prompts at natural moments |
| **Competition from Cursor/Windsurf** | Medium | Medium | .NET-first positioning; enterprise features; pipeline orchestration differentiator |
| **Scaling issues at growth** | Low | High | Azure Functions auto-scaling; database read replicas; CDN for static assets |
| **Security vulnerabilities** | Low | High | Security audit before launch; responsible disclosure program; regular dependency updates |

---

## Success Metrics

### 6-Month Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Free users** | 1,000+ | Unique authenticated accounts |
| **Paid users** | 100+ | Active Pro/Team subscriptions |
| **MRR** | $1,000+ | Monthly recurring revenue |
| **Free→Paid conversion** | 10%+ | Paid / (Free + Paid) users |
| **Monthly churn** | <5% | Canceled / Total paid users |
| **NPS score** | 40+ | Quarterly survey |

### Key Performance Indicators (KPIs)

**Acquisition:**
- Website visitors → Signups (target: 10%)
- Signups → Active users (target: 50%)
- GitHub stars (vanity but signals interest)

**Activation:**
- Time to first task completion
- Users completing 5+ tasks in first week
- Users who configure human gates

**Retention:**
- Daily/Weekly/Monthly active users
- Tasks completed per user per week
- Return rate after 7/30 days

**Revenue:**
- Average Revenue Per User (ARPU)
- Customer Lifetime Value (LTV)
- LTV:CAC ratio (target: 3:1)

---

## Infrastructure Costs (Estimated)

### MVP Infrastructure (Month 1-2)

| Service | Provider | Estimated Cost |
|---------|----------|----------------|
| License server | Azure Functions (Consumption) | $10-50/mo |
| Database | Azure SQL Basic (5 DTU) | $5-15/mo |
| Authentication | Auth0 (Free tier) | $0 |
| File storage | Azure Blob Storage | $1-5/mo |
| Monitoring | Application Insights | $0-10/mo |
| Domain + DNS | Cloudflare | $10-15/yr |
| Email (transactional) | SendGrid (Free tier) | $0 |
| **Total MVP** | | **~$25-80/mo** |

### Growth Infrastructure (Month 3-6)

| Service | Provider | Estimated Cost |
|---------|----------|----------------|
| License server | Azure Functions (Premium) | $50-150/mo |
| Database | Azure SQL S1 | $30-50/mo |
| Authentication | Auth0 (Essentials) | $23/mo |
| CDN | Azure CDN | $10-30/mo |
| Monitoring | Application Insights | $20-50/mo |
| Email | SendGrid (Essentials) | $20/mo |
| **Total Growth** | | **~$150-320/mo** |

### Payment Processing

| Provider | Fees |
|----------|------|
| Stripe | 2.9% + $0.30 per transaction |
| PayPal (optional) | 2.9% + $0.30 per transaction |

**Example at 100 Pro users ($7/mo):**
- Revenue: $700/mo
- Stripe fees: ~$50/mo (7%)
- Net revenue: ~$650/mo

---

## Implementation Timeline

### Week 1-2: Foundation

- [ ] Set up license server infrastructure
- [ ] Implement OAuth flow (GitHub)
- [ ] Create user registration/login
- [ ] Basic tier validation endpoint

### Week 3-4: Packaging

- [ ] Configure dotnet tool packaging
- [ ] Implement license check in CLI
- [ ] Add tier enforcement (task limits)
- [ ] Create update notification system

### Week 5-6: Payment & Polish

- [ ] Stripe integration
- [ ] Upgrade flow in CLI
- [ ] Landing page deployment
- [ ] Documentation site

### Week 7-8: Launch Prep

- [ ] Beta testing with early users
- [ ] Bug fixes and polish
- [ ] Prepare launch assets
- [ ] Coordinate launch timing

---

## Open Questions

1. **Pricing validation:** Should we A/B test $5 vs $7 vs $9 Pro tier?
2. **Free tier limits:** Is 5 tasks/day too generous or too restrictive?
3. **Team tier minimum:** Require minimum 3 seats or allow single-seat teams?
4. **Annual discount:** Offer 2 months free for annual billing?
5. **Student/OSS discount:** Provide free Pro tier for students and open-source maintainers?

---

## Appendix

### Competitive Landscape

| Product | Focus | Pricing | Key Difference |
|---------|-------|---------|----------------|
| Cursor | IDE with AI | $20/mo | IDE-integrated, general purpose |
| Windsurf | IDE with AI | $15/mo | IDE-integrated, general purpose |
| GitHub Copilot | Code completion | $10/mo | Inline suggestions, no orchestration |
| Continue.dev | Open source | Free | Requires setup, no pipeline |
| **Forge** | .NET orchestrator | $7/mo | Pipeline-first, human gates, enterprise |

### Glossary

- **Human Gate:** Approval checkpoint requiring human review before proceeding
- **Artifact:** Structured output from an agent (plan, research findings, etc.)
- **Worktree:** Isolated git working directory for parallel subtask execution
- **Subtask:** Child task created from splitting a parent task
- **Pipeline State:** Current stage in the task workflow

---

*Last updated: January 2026*
