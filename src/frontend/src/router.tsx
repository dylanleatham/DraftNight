import { createBrowserRouter, Navigate } from 'react-router-dom';
import { HomePage } from './pages/HomePage';
import { CreateEventPage } from './pages/CreateEventPage';
import { JoinEventPage } from './pages/JoinEventPage';
import { EventLayout } from './layouts/EventLayout';
import { LobbyPage } from './pages/LobbyPage';
import { PairingsPage } from './pages/PairingsPage';
import { StandingsPage } from './pages/StandingsPage';
import { PrizesPage } from './pages/PrizesPage';
import { AuditLogPage } from './pages/AuditLogPage';
import { LifeTrackerSetupPage } from './pages/LifeTrackerSetupPage';
import { LifeTrackerPage } from './pages/LifeTrackerPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <HomePage />,
  },
  {
    path: '/create',
    element: <CreateEventPage />,
  },
  {
    path: '/join',
    element: <JoinEventPage />,
  },
  {
    path: '/life-tracker',
    element: <LifeTrackerSetupPage />,
  },
  {
    path: '/life-tracker/game/:sessionId',
    element: <LifeTrackerPage />,
  },
  {
    path: '/event/:eventId',
    element: <EventLayout />,
    children: [
      {
        index: true,
        element: <Navigate to="lobby" replace />,
      },
      {
        path: 'lobby',
        element: <LobbyPage />,
      },
      {
        path: 'pairings',
        element: <PairingsPage />,
      },
      {
        path: 'standings',
        element: <StandingsPage />,
      },
      {
        path: 'prizes',
        element: <PrizesPage />,
      },
      {
        path: 'audit',
        element: <AuditLogPage />,
      },
      {
        path: 'match/:matchId/life',
        element: <LifeTrackerPage />,
      },
    ],
  },
]);
