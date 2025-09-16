import React, { useState, useEffect, useRef } from 'react';
import styled, { createGlobalStyle, ThemeProvider } from 'styled-components';
import { motion, AnimatePresence } from 'framer-motion';
import Select from 'react-select';
import { 
  Send, 
  Mic, 
  MicOff, 
  Volume2, 
  Play, 
  Pause, 
  Square,
  RotateCcw,
  Users,
  MessageSquare,
  Trash2,
  Bot,
  ChevronLeft,
  ChevronRight,
  Menu
} from 'lucide-react';

import ChatService from './services/ChatService';
import VoiceService from './services/VoiceService';

// Global styles
const GlobalStyle = createGlobalStyle`
  * {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
  }

  body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif;
    background: #f0f2f5;
    height: 100vh;
    overflow: hidden;
  }

  #root {
    height: 100vh;
  }
`;

// Theme - Professional Corporate Design
const theme = {
  colors: {
    primary: '#2563eb',
    primaryDark: '#1d4ed8',
    primaryLight: '#3b82f6',
    secondary: '#0891b2',
    background: '#ffffff',
    backgroundAlt: '#f8fafc',
    surface: '#ffffff',
    surfaceHover: '#f1f5f9',
    text: '#0f172a',
    textSecondary: '#475569',
    textMuted: '#94a3b8',
    border: '#e2e8f0',
    borderLight: '#f1f5f9',
    accent: '#0891b2',
    success: '#059669',
    warning: '#d97706',
    error: '#dc2626',
    voiceActive: '#dc2626',
    voiceInactive: '#6b7280',
    professional: '#1e40af',
    professionalDark: '#1e3a8a'
  },
  shadows: {
    sm: '0 1px 2px 0 rgb(0 0 0 / 0.05)',
    md: '0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)',
    lg: '0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)',
    xl: '0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)'
  },
  borderRadius: {
    sm: '4px',
    md: '8px',
    lg: '12px',
    xl: '16px',
    full: '9999px'
  }
};

// Styled components
const AppContainer = styled.div`
  display: flex;
  height: 100vh;
  background: ${props => props.theme.colors.backgroundAlt};
`;

const Sidebar = styled(motion.div)`
  width: ${props => props.collapsed ? '0px' : '320px'};
  background: ${props => props.theme.colors.surface};
  border-right: ${props => props.collapsed ? 'none' : `1px solid ${props.theme.colors.border}`};
  display: flex;
  flex-direction: column;
  box-shadow: ${props => props.collapsed ? 'none' : props.theme.shadows.lg};
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
`;

const SidebarContent = styled.div`
  width: 320px;
  display: flex;
  flex-direction: column;
  opacity: ${props => props.collapsed ? 0 : 1};
  transition: opacity 0.2s ease;
  pointer-events: ${props => props.collapsed ? 'none' : 'auto'};
`;

const SidebarToggle = styled(motion.button)`
  position: absolute;
  right: ${props => props.collapsed ? '-40px' : '-20px'};
  top: 20px;
  width: 40px;
  height: 40px;
  border-radius: ${props => props.theme.borderRadius.full};
  background: ${props => props.theme.colors.surface};
  border: 1px solid ${props => props.theme.colors.border};
  color: ${props => props.theme.colors.text};
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: ${props => props.theme.shadows.md};
  transition: all 0.3s ease;
  z-index: 10;

  &:hover {
    background: ${props => props.theme.colors.primary};
    color: white;
    transform: scale(1.05);
  }

  &:active {
    transform: scale(0.95);
  }
`;

const SidebarHeader = styled.div`
  padding: 24px;
  border-bottom: 1px solid ${props => props.theme.colors.borderLight};
  background: linear-gradient(135deg, ${props => props.theme.colors.professional} 0%, ${props => props.theme.colors.professionalDark} 100%);
  color: white;

  h1 {
    font-size: 24px;
    font-weight: 700;
    margin-bottom: 8px;
  }

  p {
    opacity: 0.9;
    font-size: 14px;
  }
`;

const AgentSection = styled.div`
  padding: 20px;
  border-bottom: 1px solid ${props => props.theme.colors.borderLight};

  h3 {
    font-size: 16px;
    font-weight: 600;
    color: ${props => props.theme.colors.text};
    margin-bottom: 12px;
    display: flex;
    align-items: center;
    gap: 8px;
  }
`;

const ChatModeSection = styled.div`
  padding: 20px;
  border-bottom: 1px solid ${props => props.theme.colors.borderLight};

  h3 {
    font-size: 16px;
    font-weight: 600;
    color: ${props => props.theme.colors.text};
    margin-bottom: 12px;
    display: flex;
    align-items: center;
    gap: 8px;
  }
`;

const ChatModeButtons = styled.div`
  display: flex;
  gap: 8px;
  margin-top: 12px;
`;

const ChatModeButton = styled(motion.button)`
  flex: 1;
  padding: 8px 12px;
  border: 1px solid ${props => props.active ? props.theme.colors.primary : props.theme.colors.border};
  border-radius: ${props => props.theme.borderRadius.md};
  background: ${props => props.active ? props.theme.colors.primary : props.theme.colors.surface};
  color: ${props => props.active ? 'white' : props.theme.colors.text};
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    background: ${props => props.active ? props.theme.colors.primaryDark : props.theme.colors.surfaceHover};
  }
`;

const VoiceSection = styled.div`
  padding: 20px;
  border-bottom: 1px solid ${props => props.theme.colors.borderLight};

  h3 {
    font-size: 16px;
    font-weight: 600;
    color: ${props => props.theme.colors.text};
    margin-bottom: 12px;
    display: flex;
    align-items: center;
    gap: 8px;
  }
`;

const VoiceControls = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
`;

const VoiceToggle = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px;
  background: ${props => props.theme.colors.backgroundAlt};
  border-radius: ${props => props.theme.borderRadius.md};
  border: 1px solid ${props => props.theme.colors.border};

  span {
    font-size: 14px;
    font-weight: 500;
    color: ${props => props.theme.colors.text};
  }
`;

const ToggleSwitch = styled(motion.button)`
  width: 52px;
  height: 28px;
  border-radius: 14px;
  background: ${props => props.active ? props.theme.colors.primary : '#cbd5e1'};
  border: 1px solid ${props => props.active ? props.theme.colors.primaryDark : '#94a3b8'};
  cursor: pointer;
  position: relative;
  transition: all 0.3s ease;
  box-shadow: ${props => props.active ? 'inset 0 1px 3px rgba(0,0,0,0.2)' : 'inset 0 1px 2px rgba(0,0,0,0.1)'};

  &:hover {
    background: ${props => props.active ? props.theme.colors.primaryDark : '#94a3b8'};
  }

  &::after {
    content: '';
    position: absolute;
    top: 2px;
    left: ${props => props.active ? '26px' : '2px'};
    width: 22px;
    height: 22px;
    border-radius: 50%;
    background: white;
    transition: all 0.3s ease;
    box-shadow: 0 2px 4px rgba(0,0,0,0.2);
  }
`;

const VoicePlaybackControls = styled.div`
  display: flex;
  gap: 12px;
  padding: 12px;
  background: ${props => props.enabled ? props.theme.colors.backgroundAlt : props.theme.colors.backgroundAlt};
  border-radius: ${props => props.theme.borderRadius.lg};
  border: 1px solid ${props => props.enabled ? props.theme.colors.border : props.theme.colors.borderLight};
  opacity: ${props => props.enabled ? 1 : 0.6};
  transition: all 0.3s ease;
`;

const VoiceButton = styled(motion.button)`
  width: 40px;
  height: 40px;
  border: 2px solid ${props => props.active ? props.theme.colors.primary : props.theme.colors.border};
  border-radius: ${props => props.theme.borderRadius.full};
  background: ${props => props.active ? props.theme.colors.primary : props.theme.colors.surface};
  color: ${props => props.active ? 'white' : props.theme.colors.text};
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  box-shadow: ${props => props.active ? props.theme.shadows.md : props.theme.shadows.sm};

  &:hover {
    background: ${props => props.active ? props.theme.colors.primaryDark : props.theme.colors.primary};
    color: white;
    border-color: ${props => props.theme.colors.primary};
    transform: translateY(-2px);
    box-shadow: ${props => props.theme.shadows.lg};
  }

  &:active {
    transform: translateY(0);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
    transform: none;
    box-shadow: none;
    &:hover {
      background: ${props => props.theme.colors.surface};
      color: ${props => props.theme.colors.textMuted};
      border-color: ${props => props.theme.colors.border};
    }
  }
`;

const MainContent = styled.div`
  flex: 1;
  display: flex;
  flex-direction: column;
  background: ${props => props.theme.colors.background};
  transition: all 0.3s ease;
`;

const ChatHeader = styled.div`
  padding: 16px 24px;
  border-bottom: 1px solid ${props => props.theme.colors.borderLight};
  background: ${props => props.theme.colors.surface};
  display: flex;
  align-items: center;
  justify-content: space-between;
  backdrop-filter: blur(10px);
  background: rgba(255, 255, 255, 0.95);

  h2 {
    font-size: 18px;
    font-weight: 600;
    color: ${props => props.theme.colors.text};
    display: flex;
    align-items: center;
    gap: 10px;
    letter-spacing: -0.02em;
  }
`;

const MenuButton = styled(motion.button)`
  width: 36px;
  height: 36px;
  border-radius: ${props => props.theme.borderRadius.md};
  background: transparent;
  border: 1px solid ${props => props.theme.colors.border};
  color: ${props => props.theme.colors.text};
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s ease;
  margin-right: 12px;

  &:hover {
    background: ${props => props.theme.colors.backgroundAlt};
    border-color: ${props => props.theme.colors.primary};
    color: ${props => props.theme.colors.primary};
  }

  &:active {
    transform: scale(0.95);
  }
`;

const ChatActions = styled.div`
  display: flex;
  gap: 8px;
`;

const ActionButton = styled(motion.button)`
  padding: 8px 16px;
  border: 1px solid ${props => props.theme.colors.border};
  border-radius: ${props => props.theme.borderRadius.md};
  background: ${props => props.theme.colors.surface};
  color: ${props => props.theme.colors.textSecondary};
  cursor: pointer;
  font-size: 13px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: all 0.3s ease;
  text-transform: uppercase;
  letter-spacing: 0.5px;

  &:hover {
    background: ${props => props.theme.colors.surfaceHover};
    transform: translateY(-1px);
  }
`;

const ChatMessages = styled.div`
  flex: 1;
  overflow-y: auto;
  padding: 32px 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  background: ${props => props.theme.colors.backgroundAlt};

  &::-webkit-scrollbar {
    width: 10px;
  }

  &::-webkit-scrollbar-track {
    background: transparent;
    border-radius: 5px;
  }

  &::-webkit-scrollbar-thumb {
    background: ${props => props.theme.colors.border};
    border-radius: 5px;
    border: 2px solid ${props => props.theme.colors.backgroundAlt};

    &:hover {
      background: ${props => props.theme.colors.textMuted};
    }
  }
`;

const Message = styled(motion.div)`
  display: flex;
  align-items: flex-start;
  gap: 12px;
  max-width: ${props => props.isUser ? '80%' : '100%'};
  margin-left: ${props => props.isUser ? 'auto' : '0'};
  flex-direction: ${props => props.isUser ? 'row-reverse' : 'row'};
`;

const MessageAvatar = styled.div`
  width: 36px;
  height: 36px;
  border-radius: ${props => props.theme.borderRadius.full};
  background: ${props => props.isUser 
    ? `linear-gradient(135deg, ${props.theme.colors.primary} 0%, ${props.theme.colors.primaryDark} 100%)`
    : `linear-gradient(135deg, ${props.theme.colors.professional} 0%, ${props.theme.colors.professionalDark} 100%)`
  };
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 13px;
  flex-shrink: 0;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
`;

const MessageContent = styled.div`
  background: ${props => props.isUser 
    ? `linear-gradient(135deg, ${props.theme.colors.primary} 0%, ${props.theme.colors.primaryDark} 100%)`
    : props.theme.colors.surface
  };
  color: ${props => props.isUser ? 'white' : props.theme.colors.text};
  padding: 14px 18px;
  border-radius: ${props => props.isUser ? '18px 18px 4px 18px' : '18px 18px 18px 4px'};
  box-shadow: ${props => props.isUser 
    ? '0 2px 12px rgba(37, 99, 235, 0.3)'
    : '0 2px 12px rgba(0, 0, 0, 0.08)'
  };
  border: ${props => props.isUser ? 'none' : `1px solid ${props.theme.colors.borderLight}`};
  max-width: 100%;
  word-wrap: break-word;
  line-height: 1.6;
  font-size: 15px;

  .message-meta {
    margin-top: 10px;
    font-size: 11px;
    opacity: ${props => props.isUser ? 0.85 : 0.6};
    display: flex;
    align-items: center;
    gap: 12px;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
`;

const ChatInput = styled.div`
  padding: 20px 24px;
  border-top: 1px solid ${props => props.theme.colors.borderLight};
  background: ${props => props.theme.colors.surface};
  box-shadow: ${props => props.theme.shadows.lg};
`;

const InputContainer = styled.div`
  display: flex;
  gap: 12px;
  align-items: flex-end;
`;

const TextInput = styled.textarea`
  flex: 1;
  padding: 14px 20px;
  border: 2px solid ${props => props.theme.colors.border};
  border-radius: 24px;
  background: ${props => props.theme.colors.surface};
  color: ${props => props.theme.colors.text};
  font-size: 15px;
  font-family: inherit;
  line-height: 1.5;
  resize: none;
  max-height: 120px;
  min-height: 48px;
  transition: all 0.3s ease;

  &:hover {
    border-color: ${props => props.theme.colors.primary};
    background: ${props => props.theme.colors.backgroundAlt};
  }

  &:focus {
    outline: none;
    border-color: ${props => props.theme.colors.primary};
    box-shadow: 0 0 0 4px ${props => props.theme.colors.primary}15;
    background: ${props => props.theme.colors.surface};
  }

  &::placeholder {
    color: ${props => props.theme.colors.textMuted};
    font-weight: 400;
  }

  &:disabled {
    background: ${props => props.theme.colors.backgroundAlt};
    cursor: not-allowed;
    opacity: 0.7;
  }
`;

const InputButtons = styled.div`
  display: flex;
  gap: 8px;
  align-items: center;
`;

const MicButton = styled(motion.button)`
  width: 48px;
  height: 48px;
  border: 2px solid ${props => props.active ? props.theme.colors.error : props.theme.colors.border};
  border-radius: ${props => props.theme.borderRadius.full};
  background: ${props => props.active 
    ? props.theme.colors.error 
    : props.theme.colors.surface
  };
  color: ${props => props.active ? 'white' : props.theme.colors.textSecondary};
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  box-shadow: ${props => props.active 
    ? '0 4px 20px rgba(220, 38, 38, 0.3)' 
    : '0 2px 8px rgba(0, 0, 0, 0.1)'
  };

  &:hover {
    transform: translateY(-2px);
    box-shadow: ${props => props.active 
      ? '0 6px 24px rgba(220, 38, 38, 0.4)' 
      : '0 4px 12px rgba(0, 0, 0, 0.15)'
    };
    background: ${props => props.active 
      ? props.theme.colors.error 
      : props.theme.colors.backgroundAlt
    };
  }

  &:active {
    transform: translateY(0);
  }
`;

const SendButton = styled(motion.button)`
  width: 48px;
  height: 48px;
  border: none;
  border-radius: ${props => props.theme.borderRadius.full};
  background: linear-gradient(135deg, ${props => props.theme.colors.primary} 0%, ${props => props.theme.colors.primaryDark} 100%);
  color: white;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  box-shadow: 0 4px 16px rgba(37, 99, 235, 0.3);
  position: relative;
  overflow: hidden;

  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: linear-gradient(135deg, ${props => props.theme.colors.primaryDark} 0%, ${props => props.theme.colors.professional} 100%);
    opacity: 0;
    transition: opacity 0.3s ease;
  }

  & > * {
    position: relative;
    z-index: 1;
  }

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 20px rgba(37, 99, 235, 0.4);
    
    &::before {
      opacity: 1;
    }
  }

  &:active {
    transform: translateY(0);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
    transform: none;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    background: ${props => props.theme.colors.textMuted};
  }
`;

const LoadingIndicator = styled(motion.div)`
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  background: white;
  border-radius: 18px 18px 18px 4px;
  color: ${props => props.theme.colors.textSecondary};
  font-size: 14px;
  font-weight: 500;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  border: 1px solid ${props => props.theme.colors.borderLight};
  max-width: fit-content;

  .dots {
    display: flex;
    gap: 3px;
  }

  .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: ${props => props.theme.colors.primary};
    animation: pulse 1.4s ease-in-out infinite both;
  }

  .dot:nth-child(1) { animation-delay: -0.32s; }
  .dot:nth-child(2) { animation-delay: -0.16s; }
  .dot:nth-child(3) { animation-delay: 0s; }

  @keyframes pulse {
    0%, 80%, 100% {
      transform: scale(0.6);
      opacity: 0.5;
    }
    40% {
      transform: scale(1);
      opacity: 1;
    }
  }
`;

// Custom Select styles
const selectStyles = {
  control: (provided, state) => ({
    ...provided,
    border: `1px solid ${state.isFocused ? theme.colors.primary : theme.colors.border}`,
    borderRadius: theme.borderRadius.md,
    boxShadow: state.isFocused ? `0 0 0 3px ${theme.colors.primary}20` : 'none',
    '&:hover': {
      borderColor: theme.colors.primary,
    },
  }),
  multiValue: (provided) => ({
    ...provided,
    backgroundColor: theme.colors.primary,
    borderRadius: theme.borderRadius.sm,
  }),
  multiValueLabel: (provided) => ({
    ...provided,
    color: 'white',
    fontSize: '12px',
  }),
  multiValueRemove: (provided) => ({
    ...provided,
    color: 'white',
    '&:hover': {
      backgroundColor: theme.colors.primaryDark,
      color: 'white',
    },
  }),
};

function App() {
  // Core state
  const [messages, setMessages] = useState([]);
  const [inputMessage, setInputMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [sessionId, setSessionId] = useState(null);
  
  // Agent and chat mode state
  const [selectedAgents, setSelectedAgents] = useState([]);
  const [availableAgents, setAvailableAgents] = useState([]);
  const [chatMode, setChatMode] = useState('single'); // 'single' or 'group'
  
  // Voice state
  const [isVoiceInputEnabled, setIsVoiceInputEnabled] = useState(false);
  const [isVoiceOutputEnabled, setIsVoiceOutputEnabled] = useState(false); // Default OFF
  const [isListening, setIsListening] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  
  // UI state
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  
  // Services
  const [chatService] = useState(() => new ChatService());
  const [voiceService] = useState(() => new VoiceService());
  
  // Refs
  const messagesEndRef = useRef(null);
  const textInputRef = useRef(null);

  // Initialize
  useEffect(() => {
    loadAvailableAgents();
    initializeVoiceService();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Auto-scroll to bottom
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const loadAvailableAgents = async () => {
    try {
      const agents = await chatService.getAvailableAgents();
      setAvailableAgents(agents);
      // Select first agent by default for single mode
      if (agents.length > 0) {
        setSelectedAgents([agents[0]]);
      }
    } catch (error) {
      console.error('Failed to load agents:', error);
    }
  };

  const initializeVoiceService = () => {
    // Set up voice input callbacks
    voiceService.onListeningStart = () => setIsListening(true);
    voiceService.onListeningEnd = () => setIsListening(false);
    voiceService.onTranscript = (transcript) => {
      if (transcript.isFinal) {
        setInputMessage(prev => prev + transcript.final + ' ');
      }
    };

    // Set up voice output callbacks
    voiceService.onSpeechStart = () => {
      setIsPlaying(true);
      setIsPaused(false);
    };
    voiceService.onSpeechEnd = () => {
      setIsPlaying(false);
      setIsPaused(false);
    };
    voiceService.onSpeechPause = () => {
      setIsPaused(true);
    };
    voiceService.onSpeechResume = () => {
      setIsPaused(false);
    };

    // Error handling
    voiceService.onError = (error) => {
      console.error('Voice service error:', error);
      setIsListening(false);
      setIsPlaying(false);
      setIsPaused(false);
    };
  };

  const handleSendMessage = async () => {
    if (!inputMessage.trim() || isLoading) return;

    const userMessage = {
      id: Date.now(),
      content: inputMessage,
      isUser: true,
      timestamp: new Date().toISOString()
    };

    setMessages(prev => [...prev, userMessage]);
    setInputMessage('');
    setIsLoading(true);

    try {
      let response;
      
      if (chatMode === 'group') {
        response = await chatService.sendGroupChatMessage(
          userMessage.content,
          sessionId
        );
      } else {
        const agentIds = selectedAgents.map(agent => agent.id);
        response = await chatService.sendMessage(
          userMessage.content,
          sessionId,
          agentIds.length > 0 ? agentIds : null
        );
      }

      const assistantMessage = {
        id: Date.now() + 1,
        content: response.content,
        isUser: false,
        timestamp: response.timestamp,
        agent: response.agent || response.speaker,
        metadata: response.metadata || {}
      };

      setMessages(prev => [...prev, assistantMessage]);
      setSessionId(response.sessionId);

      // Play voice response if enabled
      if (isVoiceOutputEnabled && response.content) {
        voiceService.speak(response.content);
      }

    } catch (error) {
      console.error('Failed to send message:', error);
      setMessages(prev => [...prev, {
        id: Date.now() + 1,
        content: `Error: ${error.message}`,
        isUser: false,
        timestamp: new Date().toISOString(),
        isError: true
      }]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const toggleVoiceInput = () => {
    if (isListening) {
      voiceService.stopListening();
    } else {
      voiceService.startListening();
    }
  };

  const toggleVoicePlayback = () => {
    if (isPlaying && !isPaused) {
      // Currently playing, so pause it
      voiceService.pauseSpeech();
    } else if (isPaused) {
      // Currently paused, so resume it
      voiceService.resumeSpeech();
    } else {
      // Not playing, replay last message if available
      replayLastMessage();
    }
  };

  const stopVoicePlayback = () => {
    voiceService.stopSpeaking();
  };

  const replayLastMessage = () => {
    const lastAssistantMessage = messages
      .filter(msg => !msg.isUser)
      .pop();
    
    if (lastAssistantMessage && isVoiceOutputEnabled) {
      voiceService.speak(lastAssistantMessage.content);
    }
  };

  const clearChat = () => {
    setMessages([]);
    setSessionId(null);
    voiceService.stopSpeaking();
  };

  const formatAgentOptions = (agents) => {
    return agents.map(agent => ({
      value: agent.id,
      label: agent.name,
      description: agent.description,
      ...agent
    }));
  };

  return (
    <ThemeProvider theme={theme}>
      <GlobalStyle />
      <AppContainer>
        <Sidebar
          collapsed={isSidebarCollapsed}
          initial={{ x: -320 }}
          animate={{ x: 0 }}
          transition={{ type: "spring", stiffness: 300, damping: 30 }}
        >
          <SidebarToggle
            collapsed={isSidebarCollapsed}
            onClick={() => setIsSidebarCollapsed(!isSidebarCollapsed)}
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            title={isSidebarCollapsed ? "Show sidebar" : "Hide sidebar"}
          >
            {isSidebarCollapsed ? <ChevronRight size={20} /> : <ChevronLeft size={20} />}
          </SidebarToggle>
          
          <SidebarContent collapsed={isSidebarCollapsed}>
            <SidebarHeader>
              <h1>Agent Chat Pro</h1>
              <p>Enterprise Multi-Agent Platform</p>
            </SidebarHeader>

            <ChatModeSection>
            <h3>
              <MessageSquare size={16} />
              Chat Mode
            </h3>
            <ChatModeButtons>
              <ChatModeButton
                active={chatMode === 'single'}
                onClick={() => setChatMode('single')}
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
              >
                Single
              </ChatModeButton>
              <ChatModeButton
                active={chatMode === 'group'}
                onClick={() => setChatMode('group')}
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
              >
                Group
              </ChatModeButton>
            </ChatModeButtons>
          </ChatModeSection>

          <AgentSection>
            <h3>
              <Bot size={16} />
              {chatMode === 'single' ? 'Select Agents' : 'Group Chat'}
            </h3>
            {chatMode === 'single' ? (
              <Select
                isMulti
                value={selectedAgents.map(agent => ({
                  value: agent.id,
                  label: agent.name,
                  ...agent
                }))}
                onChange={(selected) => setSelectedAgents(selected || [])}
                options={formatAgentOptions(availableAgents)}
                styles={selectStyles}
                placeholder="Choose agents..."
                isSearchable
                closeMenuOnSelect={false}
              />
            ) : (
              <p style={{ fontSize: '14px', color: theme.colors.textSecondary }}>
                Group chat mode uses predefined agent templates for collaborative conversations.
              </p>
            )}
          </AgentSection>

          <VoiceSection>
            <h3>
              <Volume2 size={16} />
              Voice Controls
            </h3>
            <VoiceControls>
              <VoiceToggle>
                <span>Voice Input</span>
                <ToggleSwitch
                  active={isVoiceInputEnabled}
                  onClick={() => setIsVoiceInputEnabled(!isVoiceInputEnabled)}
                  whileTap={{ scale: 0.95 }}
                />
              </VoiceToggle>
              
              <VoiceToggle>
                <span>Voice Output</span>
                <ToggleSwitch
                  active={isVoiceOutputEnabled}
                  onClick={() => {
                    const newState = !isVoiceOutputEnabled;
                    setIsVoiceOutputEnabled(newState);
                    // Stop any current speech when disabling voice output
                    if (!newState) {
                      voiceService.stopSpeaking();
                    }
                  }}
                  whileTap={{ scale: 0.95 }}
                />
              </VoiceToggle>

              {isVoiceOutputEnabled && (
                <div style={{ marginTop: '12px' }}>
                  <p style={{ fontSize: '12px', color: theme.colors.textSecondary, marginBottom: '8px', fontWeight: '500' }}>
                    Playback Controls
                  </p>
                  <VoicePlaybackControls enabled={isVoiceOutputEnabled}>
                    <VoiceButton
                      active={isPlaying && !isPaused}
                      onClick={toggleVoicePlayback}
                      disabled={!isVoiceOutputEnabled}
                      whileHover={{ scale: 1.05 }}
                      whileTap={{ scale: 0.95 }}
                      title={isPlaying && !isPaused ? "Pause" : "Play"}
                    >
                      {isPlaying && !isPaused ? <Pause size={18} /> : <Play size={18} />}
                    </VoiceButton>
                    
                    <VoiceButton
                      onClick={stopVoicePlayback}
                      disabled={!isVoiceOutputEnabled || (!isPlaying && !isPaused)}
                      whileHover={{ scale: 1.05 }}
                      whileTap={{ scale: 0.95 }}
                      title="Stop"
                    >
                      <Square size={18} />
                    </VoiceButton>
                    
                    <VoiceButton
                      onClick={replayLastMessage}
                      disabled={!isVoiceOutputEnabled || messages.filter(m => !m.isUser).length === 0}
                      whileHover={{ scale: 1.05 }}
                      whileTap={{ scale: 0.95 }}
                      title="Replay last message"
                    >
                      <RotateCcw size={18} />
                    </VoiceButton>
                  </VoicePlaybackControls>
                </div>
              )}
            </VoiceControls>
          </VoiceSection>
          </SidebarContent>
        </Sidebar>

        <MainContent>
          <ChatHeader>
            <h2>
              {isSidebarCollapsed && (
                <MenuButton
                  onClick={() => setIsSidebarCollapsed(false)}
                  whileHover={{ scale: 1.05 }}
                  whileTap={{ scale: 0.95 }}
                >
                  <Menu size={20} />
                </MenuButton>
              )}
              {chatMode === 'single' ? <Bot size={20} /> : <Users size={20} />}
              {chatMode === 'single' 
                ? `Chat with ${selectedAgents.length > 0 ? selectedAgents.map(a => a.name).join(', ') : 'AI Agent'}`
                : 'Group Chat Session'
              }
            </h2>
            <ChatActions>
              <ActionButton
                onClick={clearChat}
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
              >
                <Trash2 size={16} />
                Clear
              </ActionButton>
            </ChatActions>
          </ChatHeader>

          <ChatMessages>
            <AnimatePresence>
              {messages.map((message) => (
                <Message
                  key={message.id}
                  isUser={message.isUser}
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -20 }}
                  transition={{ duration: 0.3 }}
                >
                  <MessageAvatar isUser={message.isUser}>
                    {message.isUser ? 'U' : (message.agent ? message.agent.charAt(0).toUpperCase() : 'A')}
                  </MessageAvatar>
                  <MessageContent isUser={message.isUser}>
                    {message.content}
                    <div className="message-meta">
                      {message.agent && <span>Agent: {message.agent}</span>}
                      <span>{new Date(message.timestamp).toLocaleTimeString()}</span>
                    </div>
                  </MessageContent>
                </Message>
              ))}
            </AnimatePresence>
            
            {isLoading && (
              <LoadingIndicator
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
              >
                <Bot size={16} />
                Agent is thinking
                <div className="dots">
                  <div className="dot"></div>
                  <div className="dot"></div>
                  <div className="dot"></div>
                </div>
              </LoadingIndicator>
            )}
            
            <div ref={messagesEndRef} />
          </ChatMessages>

          <ChatInput>
            <InputContainer>
              <TextInput
                ref={textInputRef}
                value={inputMessage}
                onChange={(e) => setInputMessage(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder={isListening ? "Listening..." : "Type your message..."}
                disabled={isLoading || isListening}
              />
              
              <InputButtons>
                {isVoiceInputEnabled && (
                  <MicButton
                    active={isListening}
                    onClick={toggleVoiceInput}
                    whileHover={{ scale: 1.05 }}
                    whileTap={{ scale: 0.95 }}
                  >
                    {isListening ? <MicOff size={20} /> : <Mic size={20} />}
                  </MicButton>
                )}
                
                <SendButton
                  onClick={handleSendMessage}
                  disabled={!inputMessage.trim() || isLoading}
                  whileHover={{ scale: 1.05 }}
                  whileTap={{ scale: 0.95 }}
                >
                  <Send size={20} />
                </SendButton>
              </InputButtons>
            </InputContainer>
          </ChatInput>
        </MainContent>
      </AppContainer>
    </ThemeProvider>
  );
}

export default App;