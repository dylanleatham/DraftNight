import { test, expect, Page, BrowserContext } from '@playwright/test';

/**
 * E2E tests for the critical path of a draft night flow.
 * These tests verify the complete user journey from event creation to prize allocation.
 */

test.describe('Draft Night Flow', () => {
  test.describe('Event Creation and Lobby', () => {
    test('host can create an event and see the lobby', async ({ page }) => {
      await page.goto('/');

      // Navigate to create event
      await page.click('text=Host Event');
      await expect(page).toHaveURL('/create');

      // Fill in event details
      await page.fill('input[placeholder="Friday Night Draft"]', 'Test Draft Night');
      await page.fill('input[type="number"]', '36');
      await page.fill('input[type="password"]', 'host1234');

      // Create the event
      await page.click('button:has-text("Create Event")');

      // Should redirect to lobby
      await expect(page).toHaveURL(/\/event\/[\w-]+\/lobby/);

      // Verify lobby shows join code
      await expect(page.locator('.joinCode, [class*="joinCode"]')).toBeVisible();

      // Verify "Copy Link" button exists
      await expect(page.getByRole('button', { name: /copy link/i })).toBeVisible();

      // Start button should be disabled (need 2+ players)
      const startButton = page.getByRole('button', { name: /start event/i });
      await expect(startButton).toBeDisabled();
    });
  });

  test.describe('Player Join Flow', () => {
    test('player can join an event via join code', async ({ browser }) => {
      // Create two browser contexts for host and player
      const hostContext = await browser.newContext();
      const playerContext = await browser.newContext();
      const hostPage = await hostContext.newPage();
      const playerPage = await playerContext.newPage();

      try {
        // Host creates event
        await hostPage.goto('/create');
        await hostPage.fill('input[placeholder="Friday Night Draft"]', 'Test Event');
        await hostPage.fill('input[type="number"]', '36');
        await hostPage.fill('input[type="password"]', 'host1234');
        await hostPage.click('button:has-text("Create Event")');
        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/lobby/);

        // Get the join code
        const joinCodeElement = hostPage.locator('.joinCode, [class*="joinCode"]');
        const joinCode = await joinCodeElement.textContent();
        expect(joinCode).toBeTruthy();

        // Player joins the event
        await playerPage.goto('/join');
        await playerPage.fill('input[placeholder="ABC123"]', joinCode!);
        await playerPage.fill('input[placeholder="Enter your name"]', 'Alice');
        await playerPage.fill('input[type="password"]', 'player1234');
        await playerPage.click('button:has-text("Join Event")');

        // Player should be redirected to lobby
        await expect(playerPage).toHaveURL(/\/event\/[\w-]+\/lobby/);

        // Verify player appears in lobby
        await expect(playerPage.getByText('Alice')).toBeVisible();

        // Host should also see the player (via SignalR real-time update)
        await expect(hostPage.getByText('Alice')).toBeVisible({ timeout: 5000 });

        // Host's start button should now be enabled (1 player, but needs 2)
        // Let's add another player
        const player2Context = await browser.newContext();
        const player2Page = await player2Context.newPage();

        await player2Page.goto('/join');
        await player2Page.fill('input[placeholder="ABC123"]', joinCode!);
        await player2Page.fill('input[placeholder="Enter your name"]', 'Bob');
        await player2Page.fill('input[type="password"]', 'player5678');
        await player2Page.click('button:has-text("Join Event")');
        await expect(player2Page).toHaveURL(/\/event\/[\w-]+\/lobby/);

        // Host should see both players and start button enabled
        await expect(hostPage.getByText('Bob')).toBeVisible({ timeout: 5000 });
        const startButton = hostPage.getByRole('button', { name: /start event.*2 players/i });
        await expect(startButton).toBeEnabled();

        await player2Context.close();
      } finally {
        await hostContext.close();
        await playerContext.close();
      }
    });

    test('player can join via URL with pre-filled code', async ({ browser }) => {
      const hostContext = await browser.newContext();
      const playerContext = await browser.newContext();
      const hostPage = await hostContext.newPage();
      const playerPage = await playerContext.newPage();

      try {
        // Host creates event
        await hostPage.goto('/create');
        await hostPage.fill('input[placeholder="Friday Night Draft"]', 'URL Test Event');
        await hostPage.fill('input[type="number"]', '36');
        await hostPage.fill('input[type="password"]', 'host1234');
        await hostPage.click('button:has-text("Create Event")');
        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/lobby/);

        // Get the join code
        const joinCodeElement = hostPage.locator('.joinCode, [class*="joinCode"]');
        const joinCode = await joinCodeElement.textContent();

        // Player navigates directly with code in URL
        await playerPage.goto(`/join?code=${joinCode}`);

        // Join code should be pre-filled
        const joinCodeInput = playerPage.locator('input[placeholder="ABC123"]');
        await expect(joinCodeInput).toHaveValue(joinCode!.toUpperCase());

        // Complete joining
        await playerPage.fill('input[placeholder="Enter your name"]', 'Charlie');
        await playerPage.fill('input[type="password"]', 'player1234');
        await playerPage.click('button:has-text("Join Event")');

        await expect(playerPage).toHaveURL(/\/event\/[\w-]+\/lobby/);
        await expect(playerPage.getByText('Charlie')).toBeVisible();
      } finally {
        await hostContext.close();
        await playerContext.close();
      }
    });
  });

  test.describe('Full Tournament Flow', () => {
    test('complete 2-player round-robin tournament with prizes', async ({ browser }) => {
      // Set up host and player contexts
      const hostContext = await browser.newContext();
      const playerContext = await browser.newContext();
      const hostPage = await hostContext.newPage();
      const playerPage = await playerContext.newPage();

      try {
        // === Setup Phase ===
        // Host creates event
        await hostPage.goto('/create');
        await hostPage.fill('input[placeholder="Friday Night Draft"]', 'Tournament Test');
        await hostPage.fill('input[type="number"]', '36');
        await hostPage.fill('input[type="password"]', 'host1234');
        await hostPage.click('button:has-text("Create Event")');
        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/lobby/);

        const joinCodeElement = hostPage.locator('.joinCode, [class*="joinCode"]');
        const joinCode = await joinCodeElement.textContent();

        // Player joins
        await playerPage.goto('/join');
        await playerPage.fill('input[placeholder="ABC123"]', joinCode!);
        await playerPage.fill('input[placeholder="Enter your name"]', 'Alice');
        await playerPage.fill('input[type="password"]', 'player1234');
        await playerPage.click('button:has-text("Join Event")');
        await expect(playerPage).toHaveURL(/\/event\/[\w-]+\/lobby/);

        // Host sees player joined
        await expect(hostPage.getByText('Alice')).toBeVisible({ timeout: 5000 });

        // Host starts the event (2 players = 1 round for round-robin)
        const startButton = hostPage.getByRole('button', { name: /start event.*2 players/i });
        await expect(startButton).toBeEnabled();
        await startButton.click();

        // Should navigate to pairings
        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/pairings/);

        // === Round 1 ===
        // Verify round header
        await expect(hostPage.getByText('Round 1 of 1')).toBeVisible();
        await expect(hostPage.getByText('In Progress')).toBeVisible();

        // Player should also see pairings (navigate there)
        await playerPage.goto(hostPage.url().replace('/lobby', '/pairings'));
        await expect(playerPage.getByText('Round 1 of 1')).toBeVisible();

        // Host finalizes match - select a winner
        // Find the match card and click on one of the players to select as winner
        const matchCard = hostPage.locator('[class*="matchCard"], [class*="MatchCard"]').first();
        await expect(matchCard).toBeVisible();

        // Click on a player name/button to select winner (host sees winner selection UI)
        // The winner selection buttons should have player names
        const winnerButton = hostPage.getByRole('button', { name: 'Alice' });
        await winnerButton.click();

        // Match should now show as finalized
        await expect(hostPage.getByText('Round Closed')).toBeVisible({ timeout: 5000 });

        // === Standings Check ===
        // Navigate to standings
        await hostPage.click('a:has-text("Standings"), [href*="standings"]');
        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/standings/);

        // Verify standings show Alice with 1 win
        await expect(hostPage.getByText('Alice')).toBeVisible();
        const standingsTable = hostPage.locator('table, [class*="standings"]');
        await expect(standingsTable).toBeVisible();

        // === Prize Allocation ===
        // Navigate to prizes
        await hostPage.click('a:has-text("Prizes"), [href*="prizes"]');
        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/prizes/);

        // Should see prize allocation button
        const allocateButton = hostPage.getByRole('button', { name: /allocate prizes/i });
        await expect(allocateButton).toBeVisible();

        // Click allocate prizes
        await allocateButton.click();

        // Confirmation modal should appear
        await expect(hostPage.getByText(/are you sure you want to allocate/i)).toBeVisible();
        await hostPage.getByRole('button', { name: /allocate prizes/i }).last().click();

        // Prize display should show allocations
        await expect(hostPage.getByText(/packs?/i)).toBeVisible({ timeout: 5000 });

        // Player should also see prize allocations
        await playerPage.click('a:has-text("Prizes"), [href*="prizes"]');
        await expect(playerPage).toHaveURL(/\/event\/[\w-]+\/prizes/);
        await expect(playerPage.getByText(/packs?/i)).toBeVisible({ timeout: 5000 });
      } finally {
        await hostContext.close();
        await playerContext.close();
      }
    });

    test('3-player tournament with multiple rounds', async ({ browser }) => {
      const hostContext = await browser.newContext();
      const player1Context = await browser.newContext();
      const player2Context = await browser.newContext();
      const hostPage = await hostContext.newPage();
      const player1Page = await player1Context.newPage();
      const player2Page = await player2Context.newPage();

      try {
        // Host creates event
        await hostPage.goto('/create');
        await hostPage.fill('input[placeholder="Friday Night Draft"]', 'Multi-Round Test');
        await hostPage.fill('input[type="number"]', '36');
        await hostPage.fill('input[type="password"]', 'host1234');
        await hostPage.click('button:has-text("Create Event")');
        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/lobby/);

        const joinCode = await hostPage.locator('.joinCode, [class*="joinCode"]').textContent();

        // Players join
        await player1Page.goto('/join');
        await player1Page.fill('input[placeholder="ABC123"]', joinCode!);
        await player1Page.fill('input[placeholder="Enter your name"]', 'Alice');
        await player1Page.fill('input[type="password"]', 'player1234');
        await player1Page.click('button:has-text("Join Event")');

        await player2Page.goto('/join');
        await player2Page.fill('input[placeholder="ABC123"]', joinCode!);
        await player2Page.fill('input[placeholder="Enter your name"]', 'Bob');
        await player2Page.fill('input[type="password"]', 'player5678');
        await player2Page.click('button:has-text("Join Event")');

        // Wait for players to appear in lobby
        await expect(hostPage.getByText('Alice')).toBeVisible({ timeout: 5000 });
        await expect(hostPage.getByText('Bob')).toBeVisible({ timeout: 5000 });

        // Host starts event (3 players = 3 rounds for round-robin)
        const startButton = hostPage.getByRole('button', { name: /start event.*3 players/i });
        await startButton.click();

        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/pairings/);
        await expect(hostPage.getByText(/Round 1 of 3/)).toBeVisible();

        // Complete Round 1 - one player has BYE, finalize one match
        // Find and click winner for the non-BYE match
        const winnerButtons = hostPage.getByRole('button', { name: /(Alice|Bob)/ });
        await winnerButtons.first().click();

        // Wait for round to close
        await expect(hostPage.getByText('Round Closed')).toBeVisible({ timeout: 5000 });

        // Start Round 2
        const startRound2 = hostPage.getByRole('button', { name: /start round 2/i });
        await expect(startRound2).toBeVisible();
        await startRound2.click();

        await expect(hostPage.getByText(/Round 2 of 3/)).toBeVisible();

        // Complete Round 2
        const round2Winners = hostPage.getByRole('button', { name: /(Alice|Bob)/ });
        await round2Winners.first().click();
        await expect(hostPage.getByText('Round Closed')).toBeVisible({ timeout: 5000 });

        // Start Round 3
        const startRound3 = hostPage.getByRole('button', { name: /start round 3/i });
        await expect(startRound3).toBeVisible();
        await startRound3.click();

        await expect(hostPage.getByText(/Round 3 of 3/)).toBeVisible();

        // Complete Round 3
        const round3Winners = hostPage.getByRole('button', { name: /(Alice|Bob)/ });
        await round3Winners.first().click();
        await expect(hostPage.getByText('Round Closed')).toBeVisible({ timeout: 5000 });

        // Tournament complete message should appear
        await expect(hostPage.getByText(/tournament complete/i)).toBeVisible();

        // Navigate to prizes and allocate
        await hostPage.click('a:has-text("Prizes"), [href*="prizes"]');
        const allocateButton = hostPage.getByRole('button', { name: /allocate prizes/i });
        await allocateButton.click();
        await hostPage.getByRole('button', { name: /allocate prizes/i }).last().click();

        // Verify prizes are shown
        await expect(hostPage.getByText(/packs?/i)).toBeVisible({ timeout: 5000 });
      } finally {
        await hostContext.close();
        await player1Context.close();
        await player2Context.close();
      }
    });
  });

  test.describe('Real-time Updates', () => {
    test('player sees real-time updates when host finalizes match', async ({ browser }) => {
      const hostContext = await browser.newContext();
      const playerContext = await browser.newContext();
      const hostPage = await hostContext.newPage();
      const playerPage = await playerContext.newPage();

      try {
        // Quick setup - create event with 2 players
        await hostPage.goto('/create');
        await hostPage.fill('input[placeholder="Friday Night Draft"]', 'Realtime Test');
        await hostPage.fill('input[type="number"]', '36');
        await hostPage.fill('input[type="password"]', 'host1234');
        await hostPage.click('button:has-text("Create Event")');

        const joinCode = await hostPage.locator('.joinCode, [class*="joinCode"]').textContent();

        await playerPage.goto('/join');
        await playerPage.fill('input[placeholder="ABC123"]', joinCode!);
        await playerPage.fill('input[placeholder="Enter your name"]', 'Player1');
        await playerPage.fill('input[type="password"]', 'player1234');
        await playerPage.click('button:has-text("Join Event")');

        await expect(hostPage.getByText('Player1')).toBeVisible({ timeout: 5000 });

        // Start event
        await hostPage.getByRole('button', { name: /start event/i }).click();
        await expect(hostPage).toHaveURL(/\/event\/[\w-]+\/pairings/);

        // Player navigates to pairings
        await playerPage.click('a:has-text("Pairings"), [href*="pairings"]');
        await expect(playerPage.getByText(/Round 1/)).toBeVisible();

        // Both see "In Progress"
        await expect(hostPage.getByText('In Progress')).toBeVisible();
        await expect(playerPage.getByText('In Progress')).toBeVisible();

        // Host finalizes match
        await hostPage.getByRole('button', { name: 'Player1' }).click();

        // Player should see update via SignalR
        await expect(playerPage.getByText('Round Closed')).toBeVisible({ timeout: 5000 });
      } finally {
        await hostContext.close();
        await playerContext.close();
      }
    });
  });
});
