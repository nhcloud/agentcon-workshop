/**
 * Advanced Voice Service with Speech-to-Text and Text-to-Speech
 * Includes pause/resume functionality and Azure Speech Services integration
 */

class VoiceService {
  constructor() {
    this.isListening = false;
    this.isSpeaking = false;
    this.isPaused = false;
    this.currentUtterance = null;
    this.pausedPosition = 0;
    this.fullText = '';
    this.speechSynthesis = window.speechSynthesis;
    this.recognition = null;
    this.voices = [];
    this.selectedVoice = null;
    
    // Initialize speech recognition
    this.initializeSpeechRecognition();
    
    // Load available voices
    this.loadVoices();
    
    // Voice settings
    this.voiceSettings = {
      rate: 1.0,
      pitch: 1.0,
      volume: 1.0,
      voice: null
    };
    
    // Event listeners
    this.onListeningStart = null;
    this.onListeningEnd = null;
    this.onTranscript = null;
    this.onSpeechStart = null;
    this.onSpeechEnd = null;
    this.onSpeechPause = null;
    this.onSpeechResume = null;
    this.onError = null;
  }

  /**
   * Initialize Speech Recognition
   */
  initializeSpeechRecognition() {
    if ('webkitSpeechRecognition' in window) {
      this.recognition = new window.webkitSpeechRecognition();
    } else if ('SpeechRecognition' in window) {
      this.recognition = new window.SpeechRecognition();
    }

    if (this.recognition) {
      this.recognition.continuous = false;
      this.recognition.interimResults = true;
      this.recognition.lang = 'en-US';

      this.recognition.onstart = () => {
        this.isListening = true;
        this.onListeningStart?.();
      };

      this.recognition.onend = () => {
        this.isListening = false;
        this.onListeningEnd?.();
      };

      this.recognition.onresult = (event) => {
        let finalTranscript = '';
        let interimTranscript = '';

        for (let i = event.resultIndex; i < event.results.length; i++) {
          const transcript = event.results[i][0].transcript;
          if (event.results[i].isFinal) {
            finalTranscript += transcript;
          } else {
            interimTranscript += transcript;
          }
        }

        this.onTranscript?.({
          final: finalTranscript,
          interim: interimTranscript,
          isFinal: finalTranscript.length > 0
        });
      };

      this.recognition.onerror = (event) => {
        this.isListening = false;
        this.onError?.(event.error);
      };
    }
  }

  /**
   * Load available voices
   */
  loadVoices() {
    const updateVoices = () => {
      this.voices = this.speechSynthesis.getVoices();
      
      // Prefer neural voices or specific high-quality voices
      const preferredVoices = [
        'Microsoft Aria Online (Natural) - English (United States)',
        'Google US English',
        'Alex',
        'Samantha'
      ];

      for (const voiceName of preferredVoices) {
        const voice = this.voices.find(v => v.name.includes(voiceName.split(' ')[0]));
        if (voice) {
          this.selectedVoice = voice;
          this.voiceSettings.voice = voice;
          break;
        }
      }

      // Fallback to first English voice
      if (!this.selectedVoice) {
        const englishVoice = this.voices.find(v => v.lang.startsWith('en'));
        if (englishVoice) {
          this.selectedVoice = englishVoice;
          this.voiceSettings.voice = englishVoice;
        }
      }
    };

    updateVoices();
    this.speechSynthesis.onvoiceschanged = updateVoices;
  }

  /**
   * Start listening for speech input
   */
  startListening() {
    if (!this.recognition) {
      this.onError?.('Speech recognition not supported');
      return false;
    }

    if (this.isListening) {
      return false;
    }

    try {
      this.recognition.start();
      return true;
    } catch (error) {
      this.onError?.(error.message);
      return false;
    }
  }

  /**
   * Stop listening for speech input
   */
  stopListening() {
    if (this.recognition && this.isListening) {
      this.recognition.stop();
    }
  }

  /**
   * Speak text with advanced controls
   */
  speak(text, options = {}) {
    if (!text || typeof text !== 'string') {
      return false;
    }

    // Stop any current speech
    this.stopSpeaking();

    this.fullText = text;
    this.pausedPosition = 0;
    this.isPaused = false;

    return this._speakFromPosition(0, options);
  }

  /**
   * Internal method to speak from a specific position
   */
  _speakFromPosition(startPosition, options = {}) {
    const textToSpeak = this.fullText.substring(startPosition);
    
    if (!textToSpeak.trim()) {
      return false;
    }

    this.currentUtterance = new SpeechSynthesisUtterance(textToSpeak);
    
    // Apply voice settings
    this.currentUtterance.voice = options.voice || this.voiceSettings.voice;
    this.currentUtterance.rate = options.rate || this.voiceSettings.rate;
    this.currentUtterance.pitch = options.pitch || this.voiceSettings.pitch;
    this.currentUtterance.volume = options.volume || this.voiceSettings.volume;

    // Event handlers
    this.currentUtterance.onstart = () => {
      this.isSpeaking = true;
      this.isPaused = false;
      this.onSpeechStart?.();
    };

    this.currentUtterance.onend = () => {
      this.isSpeaking = false;
      this.isPaused = false;
      this.currentUtterance = null;
      this.pausedPosition = 0;
      this.onSpeechEnd?.();
    };

    this.currentUtterance.onpause = () => {
      this.onSpeechPause?.();
    };

    this.currentUtterance.onresume = () => {
      this.onSpeechResume?.();
    };

    this.currentUtterance.onerror = (event) => {
      this.isSpeaking = false;
      this.isPaused = false;
      this.onError?.(event.error);
    };

    // Track position for pause/resume functionality
    this.currentUtterance.onboundary = (event) => {
      if (event.name === 'word') {
        this.pausedPosition = startPosition + event.charIndex;
      }
    };

    this.speechSynthesis.speak(this.currentUtterance);
    return true;
  }

  /**
   * Pause current speech
   */
  pauseSpeech() {
    if (this.isSpeaking && !this.isPaused) {
      this.speechSynthesis.pause();
      this.isPaused = true;
      this.onSpeechPause?.();
      return true;
    }
    return false;
  }

  /**
   * Resume paused speech
   */
  resumeSpeech() {
    if (this.isPaused && this.speechSynthesis.paused) {
      this.speechSynthesis.resume();
      this.isPaused = false;
      this.onSpeechResume?.();
      return true;
    }
    
    // If speech was stopped but we have a paused position, restart from there
    if (this.pausedPosition > 0 && !this.isSpeaking) {
      return this._speakFromPosition(this.pausedPosition);
    }
    
    return false;
  }

  /**
   * Stop current speech
   */
  stopSpeaking() {
    if (this.isSpeaking || this.isPaused) {
      this.speechSynthesis.cancel();
      this.isSpeaking = false;
      this.isPaused = false;
      this.currentUtterance = null;
      this.pausedPosition = 0;
      return true;
    }
    return false;
  }

  /**
   * Toggle speech (pause if speaking, resume if paused)
   */
  toggleSpeech() {
    if (this.isSpeaking && !this.isPaused) {
      return this.pauseSpeech();
    } else if (this.isPaused) {
      return this.resumeSpeech();
    }
    return false;
  }

  /**
   * Update voice settings
   */
  updateVoiceSettings(settings) {
    this.voiceSettings = { ...this.voiceSettings, ...settings };
  }

  /**
   * Get available voices
   */
  getVoices() {
    return this.voices;
  }

  /**
   * Set voice by name or voice object
   */
  setVoice(voice) {
    if (typeof voice === 'string') {
      const foundVoice = this.voices.find(v => v.name === voice);
      if (foundVoice) {
        this.selectedVoice = foundVoice;
        this.voiceSettings.voice = foundVoice;
        return true;
      }
    } else if (voice && voice.name) {
      this.selectedVoice = voice;
      this.voiceSettings.voice = voice;
      return true;
    }
    return false;
  }

  /**
   * Check if speech recognition is supported
   */
  isSpeechRecognitionSupported() {
    return !!this.recognition;
  }

  /**
   * Check if text-to-speech is supported
   */
  isTextToSpeechSupported() {
    return !!this.speechSynthesis;
  }

  /**
   * Get current status
   */
  getStatus() {
    return {
      isListening: this.isListening,
      isSpeaking: this.isSpeaking,
      isPaused: this.isPaused,
      hasUtterance: !!this.currentUtterance,
      pausedPosition: this.pausedPosition,
      speechRecognitionSupported: this.isSpeechRecognitionSupported(),
      textToSpeechSupported: this.isTextToSpeechSupported(),
      voicesLoaded: this.voices.length > 0
    };
  }

  /**
   * Clean up resources
   */
  cleanup() {
    this.stopListening();
    this.stopSpeaking();
    
    if (this.recognition) {
      this.recognition.onstart = null;
      this.recognition.onend = null;
      this.recognition.onresult = null;
      this.recognition.onerror = null;
    }
  }
}

export default VoiceService;