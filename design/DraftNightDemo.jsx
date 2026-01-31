import React, { useState } from 'react';

// Character data with archetypes - assigned to players automatically or by selection
const CHARACTERS = [
  { id: 'shark', name: 'Card Shark', emoji: '🦈', color: '#6b21a8', accent: '#fbbf24' },
  { id: 'ruleslawyer', name: 'Rules Lawyer', emoji: '🧔', color: '#78350f', accent: '#fb923c' },
  { id: 'streamer', name: 'Streamer', emoji: '🎙️', color: '#059669', accent: '#22d3ee' },
  { id: 'animefan', name: 'Anime Fan', emoji: '🍜', color: '#db2777', accent: '#f9a8d4' },
  { id: 'strategist', name: 'Strategist', emoji: '📊', color: '#4d7c0f', accent: '#fef3c7' },
  { id: 'finalboss', name: 'Final Boss', emoji: '👑', color: '#581c87', accent: '#fde047' },
  { id: 'cosplay', name: 'Cosplayer', emoji: '⚔️', color: '#7f1d1d', accent: '#94a3b8' },
  { id: 'wildcard', name: 'Wildcard', emoji: '🃏', color: '#c2410c', accent: '#fcd34d' },
];

const PLAYER_COLORS = ['#2563eb', '#dc2626', '#16a34a', '#9333ea', '#ea580c', '#0d9488'];

// Styles embedded for artifact
const styles = {
  app: {
    minHeight: '100vh',
    background: '#151519',
    color: '#e8e8f0',
    fontFamily: '"IBM Plex Mono", monospace',
    position: 'relative',
  },
  pixelFont: {
    fontFamily: '"Press Start 2P", monospace',
  },
  displayFont: {
    fontFamily: '"VT323", monospace',
  },
  scanlines: {
    position: 'fixed',
    inset: 0,
    pointerEvents: 'none',
    background: 'repeating-linear-gradient(0deg, rgba(0,0,0,0.1), rgba(0,0,0,0.1) 1px, transparent 1px, transparent 2px)',
    zIndex: 1000,
  },
  nav: {
    background: '#0a0a0c',
    borderBottom: '3px solid #ffc53d',
    padding: '16px',
    display: 'flex',
    justifyContent: 'center',
    gap: '8px',
    flexWrap: 'wrap',
  },
  navBtn: {
    background: '#1f1f26',
    border: '2px solid #3a3a4a',
    color: '#9090a8',
    padding: '8px 16px',
    cursor: 'pointer',
    fontFamily: '"VT323", monospace',
    fontSize: '18px',
    transition: 'all 0.1s',
  },
  navBtnActive: {
    background: '#4d9fff',
    borderColor: '#2d5f9f',
    color: '#0a0a0c',
  },
  container: {
    maxWidth: '800px',
    margin: '0 auto',
    padding: '24px 16px',
  },
  panel: {
    background: '#1f1f26',
    border: '2px solid #2d2d3a',
    padding: '24px',
    marginBottom: '16px',
    boxShadow: '4px 4px 0 #0a0a0c, inset 0 0 20px rgba(0,0,0,0.5)',
  },
  btn: {
    fontFamily: '"VT323", monospace',
    fontSize: '20px',
    padding: '12px 24px',
    border: '3px solid',
    cursor: 'pointer',
    textTransform: 'uppercase',
    letterSpacing: '0.05em',
    transition: 'transform 0.05s',
  },
  btnPrimary: {
    background: '#4d9fff',
    borderColor: '#2d5f9f',
    color: '#0a0a0c',
    boxShadow: '4px 4px 0 #0a0a0c',
  },
  btnDanger: {
    background: '#ff5c5c',
    borderColor: '#a83a3a',
    color: '#0a0a0c',
  },
  btnGhost: {
    background: 'transparent',
    borderColor: '#4a4a5e',
    color: '#e8e8f0',
  },
  btnSuccess: {
    background: '#3dd97a',
    borderColor: '#2a8f52',
    color: '#0a0a0c',
  },
  input: {
    fontFamily: '"IBM Plex Mono", monospace',
    fontSize: '14px',
    padding: '12px 16px',
    background: '#0a0a0c',
    border: '2px solid #2d2d3a',
    color: '#e8e8f0',
    width: '100%',
    boxSizing: 'border-box',
  },
  badge: {
    fontFamily: '"VT323", monospace',
    fontSize: '14px',
    padding: '4px 12px',
    border: '2px solid',
    textTransform: 'uppercase',
    letterSpacing: '0.1em',
    display: 'inline-block',
  },
  title: {
    fontFamily: '"Press Start 2P", monospace',
    fontSize: '24px',
    color: '#ffc53d',
    textAlign: 'center',
    marginBottom: '8px',
    textShadow: '3px 3px 0 #0a0a0c',
  },
  subtitle: {
    fontFamily: '"VT323", monospace',
    fontSize: '24px',
    color: '#9090a8',
    textAlign: 'center',
    marginBottom: '32px',
  },
  // Character portrait styles
  portrait: {
    width: '64px',
    height: '64px',
    border: '3px solid',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '32px',
    boxShadow: '2px 2px 0 #0a0a0c',
    imageRendering: 'pixelated',
  },
  portraitSmall: {
    width: '48px',
    height: '48px',
    border: '2px solid',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '24px',
    boxShadow: '2px 2px 0 #0a0a0c',
  },
  portraitLarge: {
    width: '96px',
    height: '96px',
    border: '4px solid',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '48px',
    boxShadow: '4px 4px 0 #0a0a0c',
  },
};

// Character Portrait Component - reusable across views
const CharacterPortrait = ({ character, size = 'medium', showName = false, style = {} }) => {
  const sizeStyles = {
    small: styles.portraitSmall,
    medium: styles.portrait,
    large: styles.portraitLarge,
  };
  
  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px' }}>
      <div style={{
        ...sizeStyles[size],
        background: character.color,
        borderColor: character.accent,
        ...style,
      }}>
        {character.emoji}
      </div>
      {showName && (
        <span style={{ 
          fontSize: '10px', 
          color: character.accent, 
          fontFamily: '"VT323", monospace',
          textTransform: 'uppercase',
        }}>
          {character.name}
        </span>
      )}
    </div>
  );
};

// Home Screen
const HomeScreen = ({ onNavigate }) => (
  <div style={styles.container}>
    <div style={{ textAlign: 'center', padding: '48px 0' }}>
      <h1 style={styles.title}>⚔️ DRAFT NIGHT ⚔️</h1>
      <p style={styles.subtitle}>Manage your in-person Magic: The Gathering draft events</p>
      
      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: '300px', margin: '0 auto' }}>
        <button 
          style={{ ...styles.btn, ...styles.btnPrimary, width: '100%' }}
          onClick={() => onNavigate('create')}
        >
          Create Event
        </button>
        <button 
          style={{ ...styles.btn, ...styles.btnGhost, width: '100%' }}
          onClick={() => onNavigate('join')}
        >
          Join Event
        </button>
        <div style={{ borderTop: '2px solid #2d2d3a', margin: '16px 0' }} />
        <button 
          style={{ ...styles.btn, ...styles.btnSuccess, width: '100%' }}
          onClick={() => onNavigate('lifetracker')}
        >
          Life Tracker
        </button>
      </div>
    </div>
  </div>
);

// Create Event Screen
const CreateEventScreen = ({ onNavigate, onCreateEvent }) => {
  const [eventName, setEventName] = useState('Friday Night Draft');
  const [packs, setPacks] = useState('36');
  const [pin, setPin] = useState('');

  return (
    <div style={styles.container}>
      <button 
        style={{ ...styles.btn, ...styles.btnGhost, marginBottom: '24px', padding: '8px 16px', fontSize: '16px' }}
        onClick={() => onNavigate('home')}
      >
        ← Back
      </button>
      
      <h2 style={{ ...styles.displayFont, fontSize: '32px', marginBottom: '24px' }}>Create Event</h2>
      
      <div style={styles.panel}>
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '8px', color: '#9090a8', fontSize: '12px', textTransform: 'uppercase', letterSpacing: '0.1em' }}>
            Event Name
          </label>
          <input 
            style={styles.input}
            value={eventName}
            onChange={(e) => setEventName(e.target.value)}
            placeholder="Friday Night Draft"
          />
        </div>
        
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'block', marginBottom: '8px', color: '#9090a8', fontSize: '12px', textTransform: 'uppercase', letterSpacing: '0.1em' }}>
            Packs in Box
          </label>
          <input 
            style={styles.input}
            type="number"
            value={packs}
            onChange={(e) => setPacks(e.target.value)}
          />
        </div>
        
        <div style={{ marginBottom: '24px' }}>
          <label style={{ display: 'block', marginBottom: '8px', color: '#9090a8', fontSize: '12px', textTransform: 'uppercase', letterSpacing: '0.1em' }}>
            Host PIN
          </label>
          <input 
            style={styles.input}
            type="password"
            value={pin}
            onChange={(e) => setPin(e.target.value)}
            placeholder="At least 4 characters"
          />
        </div>
        
        <button 
          style={{ ...styles.btn, ...styles.btnPrimary, width: '100%' }}
          onClick={() => onCreateEvent(eventName)}
        >
          Create Event
        </button>
      </div>
    </div>
  );
};

// Lobby Screen
const LobbyScreen = ({ onNavigate, eventName, players }) => {
  const code = 'U79C63';
  
  return (
    <div style={styles.container}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '24px' }}>
        <div>
          <h2 style={{ ...styles.displayFont, fontSize: '28px', margin: 0 }}>{eventName}</h2>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '4px' }}>
            <span style={{ ...styles.badge, background: 'rgba(168,85,247,0.15)', color: '#a855f7', borderColor: '#6d35a0' }}>HOST</span>
            <span style={{ color: '#9090a8', fontSize: '14px' }}>Code: {code}</span>
          </div>
        </div>
        <span style={{ ...styles.badge, background: 'rgba(61,217,122,0.15)', color: '#3dd97a', borderColor: '#2a8f52' }}>● Live</span>
      </div>
      
      <div style={{ display: 'flex', gap: '16px', marginBottom: '24px' }}>
        {['Lobby', 'Pairings', 'Standings', 'Prizes', 'Audit'].map((tab, i) => (
          <button key={tab} style={{ 
            ...styles.navBtn, 
            ...(i === 0 ? styles.navBtnActive : {}),
            padding: '8px 16px',
            fontSize: '16px',
          }}>
            {tab}
          </button>
        ))}
      </div>
      
      <div style={styles.panel}>
        <div style={{ textAlign: 'center', marginBottom: '24px' }}>
          <p style={{ color: '#9090a8', marginBottom: '8px' }}>Share this code to invite players:</p>
          <div style={{ ...styles.displayFont, fontSize: '48px', color: '#4d9fff', letterSpacing: '0.2em' }}>{code}</div>
          <button style={{ ...styles.btn, ...styles.btnGhost, marginTop: '16px', padding: '8px 16px', fontSize: '16px' }}>
            Copy Link
          </button>
        </div>
        
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <span style={{ ...styles.displayFont, fontSize: '20px' }}>Players ({players.length})</span>
          <span style={{ color: '#9090a8', fontSize: '14px' }}>Min 2, Max 8</span>
        </div>
        
        {players.map((player, i) => (
          <div key={i} style={{ 
            display: 'flex', 
            justifyContent: 'space-between', 
            alignItems: 'center',
            background: '#2a2a35',
            border: '2px solid #2d2d3a',
            padding: '12px 16px',
            marginBottom: '8px',
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
              <span style={{ color: '#9090a8' }}>#{i + 1}</span>
              <CharacterPortrait character={player.character} size="small" />
              <span style={{ ...styles.displayFont, fontSize: '20px' }}>{player.name}</span>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
              <span style={{ color: '#9090a8' }}>0-0</span>
              <button style={{ ...styles.btn, ...styles.btnDanger, padding: '4px 12px', fontSize: '14px' }}>Drop</button>
            </div>
          </div>
        ))}
        
        <button 
          style={{ 
            ...styles.btn, 
            ...styles.btnPrimary, 
            width: '100%', 
            marginTop: '16px',
            opacity: players.length < 2 ? 0.5 : 1,
          }}
          onClick={() => players.length >= 2 && onNavigate('pairings')}
        >
          Start Event ({players.length} players)
        </button>
        {players.length < 2 && (
          <p style={{ textAlign: 'center', color: '#9090a8', marginTop: '8px', fontSize: '14px' }}>
            Need at least 2 players to start
          </p>
        )}
      </div>
    </div>
  );
};

// Pairings Screen - Character portraits integrated into match cards
const PairingsScreen = ({ onNavigate, players }) => {
  const [winner, setWinner] = useState(null);
  
  return (
    <div style={styles.container}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
        <div>
          <span style={{ ...styles.displayFont, fontSize: '28px' }}>Round 1 of 1</span>
          <div style={{ marginTop: '4px' }}>
            <span style={{ ...styles.badge, background: 'rgba(255,197,61,0.15)', color: '#ffc53d', borderColor: '#a88020' }}>
              {winner !== null ? 'ROUND CLOSED' : 'IN PROGRESS'}
            </span>
          </div>
        </div>
      </div>
      
      <div style={styles.panel}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <span style={{ color: '#9090a8', fontSize: '14px' }}>r1-m0</span>
          <span style={{ ...styles.badge, background: winner !== null ? 'rgba(61,217,122,0.15)' : 'rgba(255,197,61,0.15)', color: winner !== null ? '#3dd97a' : '#ffc53d', borderColor: winner !== null ? '#2a8f52' : '#a88020' }}>
            {winner !== null ? 'FINAL' : 'IN PROGRESS'}
          </span>
        </div>
        
        {/* Match card with character portraits prominently displayed */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <button 
            style={{ 
              flex: 1, 
              padding: '20px', 
              background: winner === 0 ? 'rgba(61,217,122,0.15)' : '#2a2a35',
              border: `3px solid ${winner === 0 ? '#3dd97a' : '#2d2d3a'}`,
              color: '#e8e8f0',
              cursor: 'pointer',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              gap: '12px',
            }}
            onClick={() => setWinner(0)}
          >
            <CharacterPortrait character={players[0]?.character} size="medium" />
            <span style={{ ...styles.displayFont, fontSize: '22px' }}>{players[0]?.name}</span>
            {winner === 0 && <span style={{ color: '#3dd97a', fontSize: '24px' }}>🏆 WINNER</span>}
          </button>
          
          <span style={{ ...styles.pixelFont, fontSize: '16px', color: '#9090a8' }}>VS</span>
          
          <button 
            style={{ 
              flex: 1, 
              padding: '20px', 
              background: winner === 1 ? 'rgba(61,217,122,0.15)' : '#2a2a35',
              border: `3px solid ${winner === 1 ? '#3dd97a' : '#2d2d3a'}`,
              color: '#e8e8f0',
              cursor: 'pointer',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              gap: '12px',
            }}
            onClick={() => setWinner(1)}
          >
            <CharacterPortrait character={players[1]?.character} size="medium" />
            <span style={{ ...styles.displayFont, fontSize: '22px' }}>{players[1]?.name}</span>
            {winner === 1 && <span style={{ color: '#3dd97a', fontSize: '24px' }}>🏆 WINNER</span>}
          </button>
        </div>
        
        <button 
          style={{ ...styles.btn, ...styles.btnPrimary, width: '100%', marginTop: '16px' }}
          onClick={() => onNavigate('lifetracker')}
        >
          Life Tracker
        </button>
      </div>
      
      {winner !== null && (
        <div style={{ 
          ...styles.panel, 
          background: 'rgba(61,217,122,0.1)',
          borderColor: '#3dd97a',
          textAlign: 'center',
        }}>
          <p style={{ color: '#3dd97a', margin: 0 }}>
            Tournament complete! Go to Prizes to allocate prize packs.
          </p>
        </div>
      )}
    </div>
  );
};

// Life Tracker Screen - Character portraits integrated into player panels
const LifeTrackerScreen = ({ onNavigate, players }) => {
  const [mode, setMode] = useState('draft');
  const [playerCount, setPlayerCount] = useState(2);
  const [life, setLife] = useState([20, 20, 40, 40]);
  const [gameWins, setGameWins] = useState([0, 0]);
  const [flashIndex, setFlashIndex] = useState(null);
  const [flashType, setFlashType] = useState(null);
  const [winner, setWinner] = useState(null);
  
  // Assign characters to life tracker players
  const lifeTrackerPlayers = [
    { name: 'Alex', character: CHARACTERS[0] },
    { name: 'Sam', character: CHARACTERS[1] },
    { name: 'Riley', character: CHARACTERS[2] },
    { name: 'Jordan', character: CHARACTERS[3] },
  ];
  
  const changeLife = (index, amount) => {
    const newLife = [...life];
    newLife[index] = Math.max(0, newLife[index] + amount);
    setLife(newLife);
    setFlashIndex(index);
    setFlashType(amount > 0 ? 'gain' : 'loss');
    setTimeout(() => {
      setFlashIndex(null);
      setFlashType(null);
    }, 200);
    
    if (newLife[index] === 0) {
      const winnerIndex = index === 0 ? 1 : 0;
      setWinner(winnerIndex);
    }
  };
  
  const reset = () => {
    setLife(mode === 'draft' ? [20, 20] : [40, 40, 40, 40]);
    setWinner(null);
  };
  
  const confirmWinner = () => {
    const newWins = [...gameWins];
    newWins[winner]++;
    setGameWins(newWins);
    reset();
  };
  
  const activePlayers = mode === 'draft' ? 2 : playerCount;
  
  return (
    <div style={{ ...styles.app, padding: 0 }}>
      {/* Winner Modal with character portrait */}
      {winner !== null && (
        <div style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(0,0,0,0.85)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 100,
        }}>
          <div style={{
            background: '#1f1f26',
            border: '4px solid #ffc53d',
            padding: '32px',
            textAlign: 'center',
            boxShadow: '0 0 60px rgba(255,197,61,0.4)',
          }}>
            <p style={{ ...styles.displayFont, fontSize: '20px', color: '#9090a8', margin: '0 0 16px 0' }}>MATCH WINNER</p>
            
            {/* Winner's character portrait */}
            <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '16px' }}>
              <CharacterPortrait 
                character={lifeTrackerPlayers[winner].character} 
                size="large" 
                showName={true}
              />
            </div>
            
            <p style={{ ...styles.pixelFont, fontSize: '20px', color: '#ffc53d', margin: '0 0 24px 0' }}>
              {lifeTrackerPlayers[winner].name}
            </p>
            <div style={{ display: 'flex', gap: '16px', justifyContent: 'center' }}>
              <button style={{ ...styles.btn, ...styles.btnGhost }} onClick={reset}>Reset</button>
              <button style={{ ...styles.btn, ...styles.btnPrimary }} onClick={confirmWinner}>Done</button>
            </div>
          </div>
        </div>
      )}
      
      {/* Header */}
      <div style={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        padding: '12px 16px',
        background: '#0a0a0c',
        borderBottom: '2px solid #2d2d3a',
      }}>
        <button 
          style={{ ...styles.btn, ...styles.btnGhost, padding: '8px 16px', fontSize: '14px' }}
          onClick={() => onNavigate('home')}
        >
          Exit
        </button>
        <span style={{ ...styles.displayFont, fontSize: '20px' }}>
          {mode === 'draft' ? 'Draft' : 'Commander'}
        </span>
        <button 
          style={{ ...styles.btn, ...styles.btnGhost, padding: '8px 16px', fontSize: '14px' }}
          onClick={reset}
        >
          Reset
        </button>
      </div>
      
      {/* Mode Toggle */}
      <div style={{ display: 'flex', padding: '8px', background: '#0a0a0c' }}>
        <button 
          style={{ 
            ...styles.btn, 
            flex: 1, 
            padding: '8px',
            fontSize: '14px',
            background: mode === 'draft' ? '#4d9fff' : 'transparent',
            borderColor: mode === 'draft' ? '#2d5f9f' : '#3a3a4a',
            color: mode === 'draft' ? '#0a0a0c' : '#9090a8',
          }}
          onClick={() => { setMode('draft'); setLife([20, 20]); }}
        >
          Draft (1v1)
        </button>
        <button 
          style={{ 
            ...styles.btn, 
            flex: 1, 
            padding: '8px',
            fontSize: '14px',
            background: mode === 'commander' ? '#4d9fff' : 'transparent',
            borderColor: mode === 'commander' ? '#2d5f9f' : '#3a3a4a',
            color: mode === 'commander' ? '#0a0a0c' : '#9090a8',
          }}
          onClick={() => { setMode('commander'); setLife([40, 40, 40, 40]); }}
        >
          Commander
        </button>
      </div>
      
      {/* Player Count (Commander only) */}
      {mode === 'commander' && (
        <div style={{ display: 'flex', gap: '8px', padding: '8px', background: '#0a0a0c' }}>
          {[2, 3, 4].map(n => (
            <button 
              key={n}
              style={{ 
                ...styles.btn, 
                flex: 1, 
                padding: '8px',
                fontSize: '14px',
                background: playerCount === n ? '#4d9fff' : '#1f1f26',
                borderColor: playerCount === n ? '#2d5f9f' : '#3a3a4a',
                color: playerCount === n ? '#0a0a0c' : '#9090a8',
              }}
              onClick={() => setPlayerCount(n)}
            >
              {n}
            </button>
          ))}
        </div>
      )}
      
      {/* Life Panels with Character Portraits in corners */}
      <div style={{ 
        display: 'grid', 
        gridTemplateColumns: mode === 'commander' && playerCount > 2 ? '1fr 1fr' : '1fr',
        gap: '4px',
        flex: 1,
        padding: '4px',
        background: '#0a0a0c',
      }}>
        {Array.from({ length: activePlayers }).map((_, i) => {
          const isRotated = (mode === 'draft' && i === 0) || (mode === 'commander' && i < 2 && playerCount > 2);
          const player = lifeTrackerPlayers[i];
          
          return (
            <div 
              key={i}
              style={{
                background: PLAYER_COLORS[i],
                padding: '16px',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                minHeight: mode === 'commander' && playerCount > 2 ? '200px' : '250px',
                transform: isRotated ? 'rotate(180deg)' : 'none',
                transition: 'filter 0.1s',
                position: 'relative',
                ...(flashIndex === i && flashType === 'gain' && { filter: 'brightness(1.3)' }),
                ...(flashIndex === i && flashType === 'loss' && { filter: 'brightness(0.7)' }),
              }}
            >
              {/* Character portrait positioned in corner */}
              <div style={{ 
                position: 'absolute', 
                top: isRotated ? 'auto' : '12px', 
                bottom: isRotated ? '12px' : 'auto',
                right: '12px',
                transform: isRotated ? 'rotate(180deg)' : 'none',
              }}>
                <CharacterPortrait character={player.character} size="small" />
              </div>
              
              {/* Player name */}
              <span style={{ 
                ...styles.displayFont, 
                fontSize: '18px', 
                color: 'rgba(255,255,255,0.9)',
                marginBottom: '4px',
              }}>
                {player.name}
              </span>
              
              {/* Life total */}
              <span style={{ 
                ...styles.pixelFont, 
                fontSize: mode === 'commander' && playerCount > 2 ? '40px' : '64px', 
                color: 'white',
                textShadow: '3px 3px 0 rgba(0,0,0,0.4)',
                marginBottom: '12px',
              }}>
                {life[i]}
              </span>
              
              {/* Life controls */}
              <div style={{ display: 'flex', gap: '6px' }}>
                {[-5, -1, 1, 5].map(amt => (
                  <button 
                    key={amt}
                    style={{
                      width: '44px',
                      height: '44px',
                      background: 'rgba(255,255,255,0.2)',
                      border: '2px solid rgba(255,255,255,0.3)',
                      color: 'white',
                      fontFamily: '"VT323", monospace',
                      fontSize: '18px',
                      cursor: 'pointer',
                    }}
                    onClick={() => changeLife(i, amt)}
                  >
                    {amt > 0 ? '+' : ''}{amt}
                  </button>
                ))}
              </div>
              
              {/* Poison Counter */}
              <div style={{ 
                display: 'flex', 
                alignItems: 'center', 
                gap: '8px', 
                marginTop: '12px',
                background: 'rgba(0,0,0,0.3)',
                padding: '4px 12px',
                borderRadius: '4px',
              }}>
                <span>☠️</span>
                <span style={{ color: 'white', fontFamily: '"VT323", monospace', fontSize: '16px' }}>0</span>
              </div>
            </div>
          );
        })}
      </div>
      
      {/* Game Wins with character portraits (Draft only) */}
      {mode === 'draft' && (
        <div style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          gap: '24px',
          padding: '12px',
          background: '#0a0a0c',
        }}>
          <CharacterPortrait character={lifeTrackerPlayers[0].character} size="small" />
          <span style={{ ...styles.displayFont, fontSize: '28px', color: '#e8e8f0' }}>
            {gameWins[0]} — {gameWins[1]}
          </span>
          <CharacterPortrait character={lifeTrackerPlayers[1].character} size="small" />
        </div>
      )}
    </div>
  );
};

// Main App Component
export default function DraftNightDemo() {
  const [screen, setScreen] = useState('home');
  const [eventName, setEventName] = useState('');
  const [players, setPlayers] = useState([
    { name: 'Alex', character: CHARACTERS[0] },
    { name: 'Riley', character: CHARACTERS[1] },
  ]);
  
  const handleCreateEvent = (name) => {
    setEventName(name);
    setScreen('lobby');
  };
  
  return (
    <div style={styles.app}>
      {/* Scanline overlay */}
      <div style={styles.scanlines} />
      
      {/* Navigation - No Characters tab */}
      {screen !== 'lifetracker' && (
        <nav style={styles.nav}>
          {['home', 'create', 'lobby', 'pairings', 'lifetracker'].map(s => (
            <button
              key={s}
              style={{ 
                ...styles.navBtn, 
                ...(screen === s ? styles.navBtnActive : {}),
              }}
              onClick={() => setScreen(s)}
            >
              {s === 'lifetracker' ? 'Life Tracker' : s.charAt(0).toUpperCase() + s.slice(1)}
            </button>
          ))}
        </nav>
      )}
      
      {/* Screen Content */}
      {screen === 'home' && <HomeScreen onNavigate={setScreen} />}
      {screen === 'create' && <CreateEventScreen onNavigate={setScreen} onCreateEvent={handleCreateEvent} />}
      {screen === 'join' && <CreateEventScreen onNavigate={setScreen} onCreateEvent={handleCreateEvent} />}
      {screen === 'lobby' && <LobbyScreen onNavigate={setScreen} eventName={eventName || 'Test Event'} players={players} />}
      {screen === 'pairings' && <PairingsScreen onNavigate={setScreen} players={players} />}
      {screen === 'lifetracker' && <LifeTrackerScreen onNavigate={setScreen} players={players} />}
      
      {/* Load fonts */}
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Press+Start+2P&family=VT323&family=IBM+Plex+Mono:wght@400;500;600&display=swap');
        
        button:active {
          transform: translateY(2px) !important;
        }
        
        input:focus {
          outline: none;
          border-color: #4d9fff !important;
          box-shadow: 0 0 8px #4d9fff;
        }
      `}</style>
    </div>
  );
}
