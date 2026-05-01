using System.Net;
using Discord.WebSocket;
using DNBot.Configuration;
using DNBot.Features.Levels;
using DNBot.Features.Tags;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DNBot.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboard(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Redirect("/dashboard"));
        app.MapGet("/dashboard", () => Results.Content(DashboardHtml, "text/html"));

        app.MapGet("/api/status", (DiscordSocketClient client) => new DashboardStatusResponse(
            client.CurrentUser?.Username,
            client.ConnectionState.ToString(),
            client.Guilds.Count,
            client.Latency,
            client.Guilds
                .OrderBy(guild => guild.Name)
                .Select(guild => new GuildSummary(guild.Id, guild.Name, guild.MemberCount))
                .ToArray()));

        app.MapGet("/api/settings", (BotSettingsStore settings) =>
        {
            var current = settings.Current;
            return new DashboardSettingsResponse(
                !string.IsNullOrWhiteSpace(current.Token),
                current.Prefix,
                current.DevelopmentGuildId,
                current.StatusMessages);
        });

        app.MapPut("/api/settings", (BotSettingsStore settings, DashboardSettingsRequest request) =>
        {
            var current = settings.Update(request.Token, request.Prefix, request.DevelopmentGuildId, request.StatusMessages);
            return Results.Ok(new DashboardSettingsResponse(
                !string.IsNullOrWhiteSpace(current.Token),
                current.Prefix,
                current.DevelopmentGuildId,
                current.StatusMessages));
        });

        app.MapGet("/api/guilds/{guildId}/roles", (ulong guildId, DiscordSocketClient client) =>
        {
            var guild = client.GetGuild(guildId);
            if (guild is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(guild.Roles
                .Where(role => !role.IsEveryone)
                .OrderByDescending(role => role.Position)
                .Select(role => new RoleSummary(role.Id, role.Name, role.Position, role.IsManaged))
                .ToArray());
        });

        app.MapGet("/api/guilds/{guildId}/autorole", (ulong guildId, BotSettingsStore settings) =>
            settings.GetAutoRole(guildId));

        app.MapPut("/api/guilds/{guildId}/autorole", (ulong guildId, BotSettingsStore settings, AutoRoleRequest request) =>
            Results.Ok(settings.UpsertAutoRole(new AutoRoleSettings(
                guildId,
                request.Enabled,
                request.IgnoreBots,
                request.RoleIds))));

        app.MapGet("/api/levels/{guildId}", (ulong guildId, LevelStore levels) =>
            levels.GetLeaderboard(guildId, 25));

        app.MapDelete("/api/levels/{guildId}", (ulong guildId, LevelStore levels) =>
        {
            levels.ResetGuild(guildId);
            return Results.NoContent();
        });

        app.MapDelete("/api/levels/{guildId}/{userId}", (ulong guildId, ulong userId, LevelStore levels) =>
        {
            levels.ResetUser(guildId, userId);
            return Results.NoContent();
        });

        app.MapGet("/api/tags/{guildId}", (ulong guildId, TagStore tags) => tags.List(guildId));

        app.MapDelete("/api/tags/{guildId}/{name}", (ulong guildId, string name, TagStore tags) =>
        {
            tags.TryRemove(guildId, WebUtility.UrlDecode(name), out _);
            return Results.NoContent();
        });

        return app;
    }

    private sealed record DashboardStatusResponse(
        string? Username,
        string ConnectionState,
        int Guilds,
        int LatencyMs,
        IReadOnlyList<GuildSummary> GuildList);

    private sealed record GuildSummary(ulong Id, string Name, int MemberCount);

    private sealed record RoleSummary(ulong Id, string Name, int Position, bool IsManaged);

    private sealed record DashboardSettingsResponse(
        bool HasToken,
        string Prefix,
        ulong? DevelopmentGuildId,
        IReadOnlyList<string> StatusMessages);

    private sealed record DashboardSettingsRequest(
        string? Token,
        string Prefix,
        ulong? DevelopmentGuildId,
        IReadOnlyList<string> StatusMessages);

    private sealed record AutoRoleRequest(bool Enabled, bool IgnoreBots, IReadOnlyList<ulong> RoleIds);

    private const string DashboardHtml = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>DNBot Dashboard</title>
  <style>
    :root {
      color-scheme: light;
      font-family: Inter, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif;
      --bg: #f4f7fb;
      --panel: #ffffff;
      --panel-strong: #111827;
      --line: #d9e1ee;
      --line-strong: #b7c2d4;
      --text: #152033;
      --muted: #637083;
      --blue: #2563eb;
      --blue-soft: #e7efff;
      --green: #12805c;
      --red: #dc2626;
      --amber: #a16207;
      --shadow: 0 12px 30px rgb(25 35 55 / 10%);
    }

    * { box-sizing: border-box; }
    body { margin: 0; background: var(--bg); color: var(--text); }
    button, input, select, textarea { font: inherit; }
    main { min-height: 100vh; display: grid; grid-template-columns: 260px minmax(0, 1fr); }
    aside { background: #101827; color: #f8fafc; padding: 24px 18px; }
    .brand { display: flex; align-items: center; gap: 12px; margin-bottom: 28px; }
    .mark { width: 38px; height: 38px; display: grid; place-items: center; border-radius: 8px; background: #2dd4bf; color: #0f172a; font-weight: 900; }
    .brand h1 { margin: 0; font-size: 22px; line-height: 1; }
    .brand p { margin: 3px 0 0; color: #aab7ca; font-size: 13px; }
    nav { display: grid; gap: 8px; }
    nav button { width: 100%; border: 0; border-radius: 7px; padding: 11px 12px; text-align: left; color: #dce7f7; background: transparent; cursor: pointer; }
    nav button.active, nav button:hover { background: #1d2a44; color: #ffffff; }
    .status-card { margin-top: 24px; padding: 14px; border: 1px solid #293750; border-radius: 8px; background: #152033; }
    .status-card strong { display: block; font-size: 13px; color: #aab7ca; margin-bottom: 8px; }
    .status-line { display: flex; align-items: center; gap: 8px; font-size: 14px; }
    .dot { width: 9px; height: 9px; border-radius: 999px; background: #f59e0b; }
    .dot.connected { background: #22c55e; }
    .content { padding: 26px; overflow: auto; }
    .topbar { display: flex; gap: 18px; justify-content: space-between; align-items: end; margin-bottom: 20px; }
    .topbar h2 { margin: 0; font-size: 28px; letter-spacing: 0; }
    .topbar p { margin: 4px 0 0; color: var(--muted); }
    .guild-picker { min-width: 280px; }
    .grid { display: grid; grid-template-columns: repeat(12, minmax(0, 1fr)); gap: 16px; }
    section { grid-column: span 6; background: var(--panel); border: 1px solid var(--line); border-radius: 8px; padding: 18px; box-shadow: var(--shadow); }
    section.full { grid-column: 1 / -1; }
    section.third { grid-column: span 4; }
    h3 { margin: 0 0 14px; font-size: 17px; }
    label { display: block; margin: 12px 0 6px; color: #3f4d63; font-size: 13px; font-weight: 800; }
    input, select, textarea { width: 100%; border: 1px solid var(--line-strong); border-radius: 7px; padding: 10px 12px; background: #fff; color: var(--text); }
    textarea { min-height: 116px; resize: vertical; }
    button { border: 0; border-radius: 7px; padding: 10px 14px; background: var(--blue); color: white; font-weight: 800; cursor: pointer; }
    button.secondary { background: #e8edf5; color: var(--text); }
    button.danger { background: var(--red); }
    button:disabled { cursor: not-allowed; opacity: .55; }
    .actions { display: flex; gap: 10px; flex-wrap: wrap; margin-top: 14px; }
    .muted { color: var(--muted); font-size: 14px; }
    .metric { padding: 16px; border: 1px solid var(--line); border-radius: 8px; background: #f9fbfe; }
    .metric span { display: block; color: var(--muted); font-size: 13px; font-weight: 800; }
    .metric strong { display: block; margin-top: 5px; font-size: 24px; }
    .toggle { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 12px; border: 1px solid var(--line); border-radius: 8px; background: #f9fbfe; margin-top: 10px; }
    .toggle input { width: 44px; height: 24px; accent-color: var(--blue); }
    .roles { display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 8px; max-height: 280px; overflow: auto; padding-right: 4px; }
    .role { display: flex; align-items: center; gap: 8px; border: 1px solid var(--line); border-radius: 7px; padding: 9px 10px; background: #fbfdff; }
    .role input { width: auto; }
    .swatch { width: 10px; height: 10px; border-radius: 999px; background: #8793a5; flex: 0 0 auto; }
    table { width: 100%; border-collapse: collapse; font-size: 14px; }
    th, td { text-align: left; padding: 11px 8px; border-bottom: 1px solid #e7ecf4; vertical-align: top; }
    th { color: #46556b; font-size: 12px; text-transform: uppercase; letter-spacing: .04em; }
    .pill { display: inline-flex; align-items: center; border-radius: 999px; padding: 5px 10px; background: var(--blue-soft); color: #1d4ed8; font-weight: 800; font-size: 12px; }
    .notice { border-left: 4px solid var(--amber); background: #fff8e6; padding: 11px 12px; border-radius: 6px; color: #5f4305; }
    .view { display: none; }
    .view.active { display: block; }
    .toast { position: fixed; right: 22px; bottom: 22px; min-width: 240px; border-radius: 8px; padding: 12px 14px; background: #111827; color: white; box-shadow: var(--shadow); opacity: 0; transform: translateY(8px); pointer-events: none; transition: .18s ease; }
    .toast.show { opacity: 1; transform: translateY(0); }

    @media (max-width: 900px) {
      main { display: block; }
      aside { position: static; }
      .content { padding: 18px; }
      .topbar { display: block; }
      .guild-picker { margin-top: 14px; min-width: 0; }
      section, section.third { grid-column: 1 / -1; }
    }
  </style>
</head>
<body>
  <main>
    <aside>
      <div class="brand">
        <div class="mark">DN</div>
        <div>
          <h1>DNBot</h1>
          <p>Control center</p>
        </div>
      </div>
      <nav>
        <button class="active" data-view="overview">Overview</button>
        <button data-view="setup">Setup</button>
        <button data-view="autorole">Autorole</button>
        <button data-view="levels">Levels</button>
        <button data-view="tags">Tags</button>
      </nav>
      <div class="status-card">
        <strong>Discord Status</strong>
        <div class="status-line"><span class="dot" id="statusDot"></span><span id="statusText">Loading...</span></div>
        <p class="muted" id="statusMeta"></p>
      </div>
    </aside>

    <div class="content">
      <div class="topbar">
        <div>
          <h2 id="pageTitle">Overview</h2>
          <p>The dashboard is the primary configuration surface. Env vars are optional for deployments.</p>
        </div>
        <div class="guild-picker">
          <label for="guildSelect">Active Server</label>
          <select id="guildSelect"></select>
        </div>
      </div>

      <div id="overview" class="view active">
        <div class="grid">
          <section class="third metric"><span>Connection</span><strong id="metricConnection">Offline</strong></section>
          <section class="third metric"><span>Servers</span><strong id="metricGuilds">0</strong></section>
          <section class="third metric"><span>Latency</span><strong id="metricLatency">0ms</strong></section>
          <section class="full">
            <h3>Quick Start</h3>
            <p class="notice">Use Setup to save the token, prefix, development guild, and statuses. Use Autorole to pick join roles for the selected server.</p>
          </section>
        </div>
      </div>

      <div id="setup" class="view">
        <div class="grid">
          <section>
            <h3>Bot Settings</h3>
            <label for="token">Bot Token</label>
            <input id="token" type="password" placeholder="Leave blank to keep existing token">
            <p class="muted" id="tokenState"></p>
            <label for="prefix">Prefix</label>
            <input id="prefix" maxlength="8">
            <label for="devGuild">Development Guild</label>
            <select id="devGuild"></select>
            <div class="actions">
              <button onclick="saveSettings()">Save Settings</button>
              <button class="secondary" onclick="loadSettings()">Reload</button>
            </div>
            <p class="notice">These settings are saved to <strong>data/settings.json</strong>. Existing dashboard settings win at runtime.</p>
          </section>
          <section>
            <h3>Status Rotation</h3>
            <label for="statuses">Status Messages</label>
            <textarea id="statuses" placeholder="One status per line"></textarea>
            <p class="muted">Prefix and statuses apply while running. Token and slash command guild changes apply on restart.</p>
          </section>
          <section class="full">
            <h3>Manual Environment Setup</h3>
            <p class="muted">Use environment variables only when you want scripts, containers, or hosting platforms to seed first-run settings.</p>
            <table>
              <thead><tr><th>Variable</th><th>Purpose</th></tr></thead>
              <tbody>
                <tr><td><code>DNBOT_Discord__Token</code></td><td>Bot token used when <code>data/settings.json</code> does not exist yet.</td></tr>
                <tr><td><code>DNBOT_Discord__Prefix</code></td><td>Initial prefix for text commands.</td></tr>
                <tr><td><code>DNBOT_Discord__DevelopmentGuildId</code></td><td>Initial guild for fast slash-command registration.</td></tr>
                <tr><td><code>Dashboard__Url</code></td><td>Optional dashboard bind URL, such as <code>http://localhost:5080</code>.</td></tr>
              </tbody>
            </table>
          </section>
        </div>
      </div>

      <div id="autorole" class="view">
        <div class="grid">
          <section>
            <h3>Autorole Rules</h3>
            <div class="toggle">
              <div><strong>Enable autorole</strong><div class="muted">Assign selected roles to new members.</div></div>
              <input id="autoroleEnabled" type="checkbox">
            </div>
            <div class="toggle">
              <div><strong>Ignore bots</strong><div class="muted">Recommended for integration accounts.</div></div>
              <input id="autoroleIgnoreBots" type="checkbox">
            </div>
            <div class="actions">
              <button onclick="saveAutoRole()">Save Autorole</button>
              <button class="secondary" onclick="loadAutoRole()">Reload</button>
            </div>
          </section>
          <section>
            <h3>Assignable Roles</h3>
            <div id="roles" class="roles"></div>
            <p class="muted">The bot can only assign roles below its highest role in Discord.</p>
          </section>
        </div>
      </div>

      <div id="levels" class="view">
        <div class="grid">
          <section class="full">
            <div class="actions">
              <button onclick="loadLevels()">Refresh Levels</button>
              <button class="danger" onclick="resetGuildLevels()">Reset Server Levels</button>
            </div>
            <table>
              <thead><tr><th>User ID</th><th>Level</th><th>XP</th><th>Next</th><th></th></tr></thead>
              <tbody id="levelRows"></tbody>
            </table>
          </section>
        </div>
      </div>

      <div id="tags" class="view">
        <div class="grid">
          <section class="full">
            <div class="actions"><button onclick="loadTags()">Refresh Tags</button></div>
            <table>
              <thead><tr><th>Name</th><th>Owner</th><th>Content</th><th></th></tr></thead>
              <tbody id="tagRows"></tbody>
            </table>
          </section>
        </div>
      </div>
    </div>
  </main>

  <div class="toast" id="toast"></div>

  <script>
    const state = { guilds: [], selectedGuild: null, roles: [], autorole: null };

    const $ = id => document.getElementById(id);
    const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

    async function getJson(url) {
      const response = await fetch(url);
      if (!response.ok) throw new Error(await response.text());
      return response.json();
    }

    async function sendJson(url, method, body) {
      const response = await fetch(url, { method, headers: { 'content-type': 'application/json' }, body: JSON.stringify(body) });
      if (!response.ok) throw new Error(await response.text());
      return response.status === 204 ? null : response.json();
    }

    function toast(message) {
      $('toast').textContent = message;
      $('toast').classList.add('show');
      setTimeout(() => $('toast').classList.remove('show'), 2200);
    }

    function selectedGuildId() {
      const value = $('guildSelect').value;
      if (!value) throw new Error('Select a server first.');
      return value;
    }

    async function loadStatus() {
      const status = await getJson('/api/status');
      state.guilds = status.guildList;
      $('statusText').textContent = status.username ? `${status.username} · ${status.connectionState}` : status.connectionState;
      $('statusMeta').textContent = `${status.guilds} servers · ${status.latencyMs}ms`;
      $('statusDot').classList.toggle('connected', status.connectionState === 'Connected');
      $('metricConnection').textContent = status.connectionState;
      $('metricGuilds').textContent = status.guilds;
      $('metricLatency').textContent = `${status.latencyMs}ms`;
      renderGuildSelects();
    }

    function renderGuildSelects() {
      const options = ['<option value="">Select server...</option>'].concat(state.guilds.map(g => `<option value="${g.id}">${esc(g.name)} (${g.memberCount})</option>`)).join('');
      const guildSelect = $('guildSelect');
      const devGuild = $('devGuild');
      const current = guildSelect.value || state.selectedGuild;
      guildSelect.innerHTML = options;
      devGuild.innerHTML = '<option value="">Global commands</option>' + state.guilds.map(g => `<option value="${g.id}">${esc(g.name)}</option>`).join('');
      if (current) guildSelect.value = current;
      state.selectedGuild = guildSelect.value || null;
    }

    async function loadSettings() {
      const settings = await getJson('/api/settings');
      $('token').value = '';
      $('tokenState').textContent = settings.hasToken ? 'A token is saved locally.' : 'No token saved yet.';
      $('prefix').value = settings.prefix;
      $('devGuild').value = settings.developmentGuildId ?? '';
      $('statuses').value = settings.statusMessages.join('\n');
    }

    async function saveSettings() {
      await sendJson('/api/settings', 'PUT', {
        token: $('token').value || null,
        prefix: $('prefix').value,
        developmentGuildId: $('devGuild').value || null,
        statusMessages: $('statuses').value.split('\n').map(x => x.trim()).filter(Boolean)
      });
      await loadSettings();
      toast('Settings saved');
    }

    async function loadRoles() {
      const guild = selectedGuildId();
      state.roles = await getJson(`/api/guilds/${guild}/roles`);
      renderRoles();
    }

    async function loadAutoRole() {
      const guild = selectedGuildId();
      const [autorole] = await Promise.all([
        getJson(`/api/guilds/${guild}/autorole`),
        loadRoles()
      ]);
      state.autorole = autorole;
      $('autoroleEnabled').checked = autorole.enabled;
      $('autoroleIgnoreBots').checked = autorole.ignoreBots;
      renderRoles();
    }

    function renderRoles() {
      const selected = new Set((state.autorole?.roleIds ?? []).map(String));
      $('roles').innerHTML = state.roles.map(role => `
        <label class="role">
          <input type="checkbox" value="${role.id}" ${selected.has(String(role.id)) ? 'checked' : ''} ${role.isManaged ? 'disabled' : ''}>
          <span class="swatch"></span>
          <span>${esc(role.name)}</span>
        </label>`).join('') || '<p class="muted">No roles loaded. Select a connected server.</p>';
    }

    async function saveAutoRole() {
      const guild = selectedGuildId();
      const roleIds = Array.from(document.querySelectorAll('#roles input:checked')).map(input => input.value);
      state.autorole = await sendJson(`/api/guilds/${guild}/autorole`, 'PUT', {
        enabled: $('autoroleEnabled').checked,
        ignoreBots: $('autoroleIgnoreBots').checked,
        roleIds
      });
      await loadAutoRole();
      toast('Autorole saved');
    }

    async function loadLevels() {
      const rows = await getJson(`/api/levels/${selectedGuildId()}`);
      $('levelRows').innerHTML = rows.map(row => `
        <tr>
          <td>${row.userId}</td><td><span class="pill">${row.level}</span></td><td>${row.xp}</td><td>${row.xpForNextLevel}</td>
          <td><button class="danger" onclick="resetUserLevel('${row.guildId}', '${row.userId}')">Reset</button></td>
        </tr>`).join('') || '<tr><td colspan="5" class="muted">No level data yet.</td></tr>';
    }

    async function resetUserLevel(guild, user) {
      await fetch(`/api/levels/${guild}/${user}`, { method: 'DELETE' });
      await loadLevels();
      toast('User level reset');
    }

    async function resetGuildLevels() {
      await fetch(`/api/levels/${selectedGuildId()}`, { method: 'DELETE' });
      await loadLevels();
      toast('Server levels reset');
    }

    async function loadTags() {
      const rows = await getJson(`/api/tags/${selectedGuildId()}`);
      $('tagRows').innerHTML = rows.map(row => `
        <tr>
          <td><span class="pill">${esc(row.name)}</span></td><td>${row.ownerId}</td><td>${esc(row.content)}</td>
          <td><button class="danger" onclick="deleteTag('${row.guildId}', '${encodeURIComponent(row.name)}')">Delete</button></td>
        </tr>`).join('') || '<tr><td colspan="4" class="muted">No tags yet.</td></tr>';
    }

    async function deleteTag(guild, name) {
      await fetch(`/api/tags/${guild}/${name}`, { method: 'DELETE' });
      await loadTags();
      toast('Tag deleted');
    }

    document.querySelectorAll('nav button').forEach(button => {
      button.addEventListener('click', () => {
        document.querySelectorAll('nav button').forEach(item => item.classList.remove('active'));
        document.querySelectorAll('.view').forEach(item => item.classList.remove('active'));
        button.classList.add('active');
        $(button.dataset.view).classList.add('active');
        $('pageTitle').textContent = button.textContent;
      });
    });

    $('guildSelect').addEventListener('change', async () => {
      state.selectedGuild = $('guildSelect').value || null;
      if (!state.selectedGuild) return;
      try {
        await Promise.all([loadAutoRole(), loadLevels(), loadTags()]);
      } catch (error) {
        toast('Could not load server data');
      }
    });

    (async function init() {
      try {
        await loadStatus();
        await loadSettings();
        if (state.guilds.length > 0) {
          $('guildSelect').value = state.guilds[0].id;
          $('guildSelect').dispatchEvent(new Event('change'));
        }
        setInterval(loadStatus, 5000);
      } catch (error) {
        toast('Dashboard failed to load');
      }
    })();
  </script>
</body>
</html>
""";
}
