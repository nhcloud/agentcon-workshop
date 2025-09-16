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
  Bot
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
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    height: 100vh;
    overflow: hidden;
  }

  #root {
    height: 100vh;
  }
`;

// Theme
const theme = {
  colors: {
    primary: '#4f46e5',
    primaryDark: '#3730a3',
    secondary: '#10b981',
    background: '#ffffff',
    backgroundAlt: '#f8fafc',
    surface: '#ffffff',
    surfaceHover: '#f1f5f9',
    text: '#1e293b',
    textSecondary: '#64748b',
    textMuted: '#94a3b8',
    border: '#e2e8f0',
    borderLight: '#f1f5f9',
    accent: '#f59e0b',
    success: '#10b981',
    warning: '#f59e0b',
    error: '#ef4444',
    voiceActive: '#ef4444',
    voiceInactive: '#6b7280'
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
  width: 320px;
  background: ${props => props.theme.colors.surface};
  border-right: 1px solid ${props => props.theme.colors.border};
  display: flex;
  flex-direction: column;
  box-shadow: ${props => props.theme.shadows.lg};
`;

const SidebarHeader = styled.div`
  padding: 24px;
  border-bottom: 1px solid ${props => props.theme.colors.borderLight};
  background: linear-gradient(135deg, ${props => props.theme.colors.primary} 0%, ${props => props.theme.colors.primaryDark} 100%);
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
  width: 48px;
  height: 24px;
  border-radius: 12px;
  background: ${props => props.active ? props.theme.colors.primary : props.theme.colors.border};
  border: none;
  cursor: pointer;
  position: relative;
  transition: background-color 0.2s ease;

  &::after {
    content: '';
    position: absolute;
    top: 2px;
    left: ${props => props.active ? '26px' : '2px'};
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: white;
    transition: left 0.2s ease;
    box-shadow: ${props => props.theme.shadows.sm};
  }
`;

const VoicePlaybackControls = styled.div`
  display: flex;
  gap: 8px;
  opacity: ${props => props.enabled ? 1 : 0.5};
  transition: opacity 0.2s ease;
`;

const VoiceButton = styled(motion.button)`
  padding: 8px;
  border: 1px solid ${props => props.theme.colors.border};
  border-radius: ${props => props.theme.borderRadius.md};
  background: ${props => props.active ? props.theme.colors.primary : props.theme.colors.surface};
  color: ${props => props.active ? 'white' : props.theme.colors.text};
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;

  &:hover {
    background: ${props => props.active ? props.theme.colors.primaryDark : props.theme.colors.surfaceHover};
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

const MainContent = styled.div`
  flex: 1;
  display: flex;
  flex-direction: column;
  background: ${props => props.theme.colors.background};
`;

const ChatHeader = styled.div`
  padding: 20px 24px;
  border-bottom: 1px solid ${props => props.theme.colors.borderLight};
  background: ${props => props.theme.colors.surface};
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: ${props => props.theme.shadows.sm};

  h2 {
    font-size: 20px;
    font-weight: 600;
    color: ${props => props.theme.colors.text};
    display: flex;
    align-items: center;
    gap: 8px;
  }
`;

const ChatActions = styled.div`
  display: flex;
  gap: 8px;
`;

const ActionButton = styled(motion.button)`
  padding: 8px 12px;
  border: 1px solid ${props => props.theme.colors.border};
  border-radius: ${props => props.theme.borderRadius.md};
  background: ${props => props.theme.colors.surface};
  color: ${props => props.theme.colors.text};
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;

  &:hover {
    background: ${props => props.theme.colors.surfaceHover};
    transform: translateY(-1px);
  }
`;

const ChatMessages = styled.div`
  flex: 1;
  overflow-y: auto;
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;

  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-track {
    background: ${props => props.theme.colors.backgroundAlt};
  }

  &::-webkit-scrollbar-thumb {
    background: ${props => props.theme.colors.border};
    border-radius: 3px;
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
  width: 40px;
  height: 40px;
  border-radius: ${props => props.theme.borderRadius.full};
  background: ${props => props.isUser ? props.theme.colors.primary : props.theme.colors.secondary};
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 14px;
  flex-shrink: 0;
`;

const MessageContent = styled.div`
  background: ${props => props.isUser ? props.theme.colors.primary : props.theme.colors.surface};
  color: ${props => props.isUser ? 'white' : props.theme.colors.text};
  padding: 16px 20px;
  border-radius: ${props => props.theme.borderRadius.lg};
  box-shadow: ${props => props.theme.shadows.md};
  max-width: 100%;
  word-wrap: break-word;
  line-height: 1.6;

  .message-meta {
    margin-top: 8px;
    font-size: 12px;
    opacity: 0.7;
    display: flex;
    align-items: center;
    gap: 8px;
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
  padding: 16px 20px;
  border: 1px solid ${props => props.theme.colors.border};
  border-radius: ${props => props.theme.borderRadius.lg};
  background: ${props => props.theme.colors.background};
  color: ${props => props.theme.colors.text};
  font-size: 16px;
  line-height: 1.5;
  resize: none;
  max-height: 120px;
  min-height: 52px;
  transition: all 0.2s ease;

  &:focus {
    outline: none;
    border-color: ${props => props.theme.colors.primary};
    box-shadow: 0 0 0 3px ${props => props.theme.colors.primary}20;
  }

  &::placeholder {
    color: ${props => props.theme.colors.textMuted};
  }
`;

const InputButtons = styled.div`
  display: flex;
  gap: 8px;
  align-items: center;
`;

const MicButton = styled(motion.button)`
  width: 52px;
  height: 52px;
  border: none;
  border-radius: ${props => props.theme.borderRadius.full};
  background: ${props => props.active ? props.theme.colors.voiceActive : props.theme.colors.textMuted};
  color: white;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  box-shadow: ${props => props.theme.shadows.md};

  &:hover {
    transform: scale(1.05);
  }

  &:active {
    transform: scale(0.95);
  }
`;

const SendButton = styled(motion.button)`
  width: 52px;
  height: 52px;
  border: none;
  border-radius: ${props => props.theme.borderRadius.full};
  background: ${props => props.theme.colors.primary};
  color: white;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  box-shadow: ${props => props.theme.shadows.md};

  &:hover {
    background: ${props => props.theme.colors.primaryDark};
    transform: scale(1.05);
  }

  &:active {
    transform: scale(0.95);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
    transform: none;
  }
`;

const LoadingIndicator = styled(motion.div)`
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 16px 20px;
  background: ${props => props.theme.colors.backgroundAlt};
  border-radius: ${props => props.theme.borderRadius.lg};
  color: ${props => props.theme.colors.textSecondary};
  font-size: 14px;
  box-shadow: ${props => props.theme.shadows.sm};

  .dots {
    display: flex;
    gap: 4px;
  }

  .dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: ${props => props.theme.colors.textMuted};
    animation: pulse 1.4s ease-in-out infinite both;
  }

  .dot:nth-child(1) { animation-delay: -0.32s; }
  .dot:nth-child(2) { animation-delay: -0.16s; }

  @keyframes pulse {
    0%, 80%, 100% {
      transform: scale(0);
    }
    40% {
      transform: scale(1);
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
  const [isVoiceOutputEnabled, setIsVoiceOutputEnabled] = useState(true);
  const [isListening, setIsListening] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  
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
          initial={{ x: -320 }}
          animate={{ x: 0 }}
          transition={{ type: "spring", stiffness: 300, damping: 30 }}
        >
          <SidebarHeader>
            <h1>AI Agents Workshop</h1>
            <p>Professional Multi-Agent Chat Interface</p>
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

              <VoicePlaybackControls enabled={isVoiceOutputEnabled}>
                <VoiceButton
                  active={isPlaying && !isPaused}
                  onClick={toggleVoicePlayback}
                  disabled={!isVoiceOutputEnabled}
                  whileHover={{ scale: 1.05 }}
                  whileTap={{ scale: 0.95 }}
                  title={isPlaying && !isPaused ? "Pause" : "Resume"}
                >
                  {isPlaying && !isPaused ? <Pause size={16} /> : <Play size={16} />}
                </VoiceButton>
                
                <VoiceButton
                  onClick={stopVoicePlayback}
                  disabled={!isVoiceOutputEnabled || (!isPlaying && !isPaused)}
                  whileHover={{ scale: 1.05 }}
                  whileTap={{ scale: 0.95 }}
                  title="Stop"
                >
                  <Square size={16} />
                </VoiceButton>
                
                <VoiceButton
                  onClick={replayLastMessage}
                  disabled={!isVoiceOutputEnabled}
                  whileHover={{ scale: 1.05 }}
                  whileTap={{ scale: 0.95 }}
                  title="Replay last message"
                >
                  <RotateCcw size={16} />
                </VoiceButton>
              </VoicePlaybackControls>
            </VoiceControls>
          </VoiceSection>
        </Sidebar>

        <MainContent>
          <ChatHeader>
            <h2>
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