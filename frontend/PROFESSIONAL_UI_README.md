# Professional Multi-Agent Chat Interface

This is a completely rebuilt, professional React frontend for the AI Agents Workshop with advanced voice capabilities and multi-agent support.

## ✨ Features

### 🎭 Multi-Agent Support
- **Single Agent Mode**: Select one or multiple specific agents for targeted conversations
- **Group Chat Mode**: Use predefined agent templates for collaborative AI conversations
- **Dynamic Agent Selection**: Choose from available agents with real-time updates

### 🎤 Advanced Voice Features
- **Speech-to-Text**: Real-time voice input with Web Speech API
- **Text-to-Speech**: High-quality voice output with natural speech synthesis
- **Voice Controls**: 
  - Enable/disable voice input and output independently
  - Pause/resume voice playback from where it left off
  - Replay last assistant message
  - Visual feedback for recording and playback states

### 🎨 Professional UI Design
- **Modern Design**: Clean, professional interface with smooth animations
- **Responsive Layout**: Sidebar navigation with collapsible sections
- **Theme System**: Consistent color scheme and typography
- **Micro-interactions**: Smooth hover effects and transitions
- **Loading States**: Professional loading indicators and typing animations

### 🛠 Technical Features
- **React 18**: Latest React with hooks and functional components
- **Styled Components**: CSS-in-JS with theme support
- **Framer Motion**: Smooth animations and transitions
- **React Select**: Advanced multi-select dropdown for agent selection
- **Lucide React**: Modern, consistent icon library
- **Real-time Communication**: WebSocket-ready architecture
- **Error Handling**: Comprehensive error states and retry mechanisms

## 🏗 Architecture

### Frontend Structure
```
src/
├── App.js                 # Main application with all UI components
├── services/
│   ├── ChatService.js     # Enhanced API communication with multi-agent support
│   └── VoiceService.js    # Advanced speech recognition and synthesis
└── index.js              # Application entry point
```

### Key Components
- **Sidebar**: Agent selection, chat mode switching, voice controls
- **Chat Area**: Message display with professional styling and animations
- **Input System**: Multi-modal input (text + voice) with advanced controls
- **Voice System**: Complete speech-to-text and text-to-speech pipeline

## 🎯 Usage

### Chat Modes
1. **Single Agent Mode**: 
   - Select one or more specific agents
   - Messages route to chosen agents
   - Supports multi-agent conversations

2. **Group Chat Mode**:
   - Uses predefined agent templates
   - Collaborative AI conversations
   - Automatic agent orchestration

### Voice Features
1. **Voice Input**:
   - Toggle voice input on/off
   - Click microphone button to start/stop listening
   - Real-time transcription appears in text input
   - Visual feedback during recording

2. **Voice Output**:
   - Toggle voice output on/off
   - Automatic speech for all assistant responses
   - Pause/resume playback controls
   - Replay functionality for last message

### Professional Features
- **Smooth Animations**: All UI elements have polished transitions
- **Loading States**: Professional indicators during AI processing
- **Error Handling**: Graceful error messages and recovery
- **Responsive Design**: Works on various screen sizes
- **Accessibility**: Keyboard navigation and screen reader support

## 🔧 Configuration

### Environment Setup
- Frontend runs on port 3001
- backend should be running on port 8000 (LangChain) or 8001 (Semantic Kernel)
- Voice features require HTTPS for production (browser security)

### Voice Configuration
The VoiceService automatically detects:
- Available speech synthesis voices
- Speech recognition language settings
- Browser speech API capabilities

### API Integration
ChatService handles:
- Multi-agent message routing
- Group chat orchestration
- Session management
- Error recovery
- Health checks

## 🚀 Getting Started

1. **Install Dependencies**:
   ```bash
   cd frontend
   npm install
   ```

2. **Start Development Server**:
   ```bash
   npm start
   ```

3. **Access Application**:
   Open http://localhost:3001

4. **Start backend Services**:
   Make sure your backend agents are running on the expected ports

## 🎨 Customization

### Theme
The application uses a comprehensive theme system in `App.js`:
- Colors: Primary, secondary, background, text variants
- Shadows: Multiple shadow levels for depth
- Border radius: Consistent rounded corners
- Typography: Professional font stack

### Voice Settings
Voice features can be customized by modifying `VoiceService.js`:
- Speech recognition language
- Voice synthesis settings
- Pause/resume behavior
- Audio quality settings

### Agent Configuration
Agent selection can be customized in the ChatService:
- Add new agent types
- Modify agent descriptions
- Configure routing logic
- Add agent-specific features

## 🔍 Advanced Features

### Real-time Voice Control
- **Continuous Recognition**: Keeps listening until manually stopped
- **Interim Results**: Shows partial transcription while speaking
- **Noise Handling**: Filters out background noise and silent periods
- **Position Tracking**: Remembers playback position for pause/resume

### Professional UI Elements
- **Gradient Backgrounds**: Subtle gradients for depth
- **Motion Design**: Carefully crafted animation timings
- **Focus States**: Clear focus indicators for accessibility
- **Hover Effects**: Subtle micro-interactions
- **Loading Animations**: Professional pulse and fade effects

### Error Recovery
- **Network Resilience**: Automatic retry on connection failures
- **Graceful Degradation**: Voice features fall back gracefully
- **User Feedback**: Clear error messages and recovery suggestions
- **State Recovery**: Maintains chat state across errors

This interface represents a complete overhaul of the original chat application with enterprise-grade features and professional polish.
