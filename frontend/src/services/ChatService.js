import axios from 'axios';

const API_BASE_URL = process.env.NODE_ENV === 'production' 
  ? '' 
  : 'http://localhost:8000';

class ChatService {
  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      timeout: 120000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor
    this.api.interceptors.request.use(
      (config) => {
        console.log('Making request to:', config.url);
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor
    this.api.interceptors.response.use(
      (response) => {
        return response;
      },
      (error) => {
        console.error('API Error:', error.response?.data || error.message);
        return Promise.reject(error);
      }
    );
  }

  /**
   * Send message to single agent or multiple agents
   */
  async sendMessage(message, sessionId = null, agents = null) {
    try {
      // Handle single agent (backward compatibility)
      if (typeof agents === 'string') {
        agents = [agents];
      }

      const response = await this.api.post('/chat', {
        message,
        session_id: sessionId,
        agents: agents // Array of agent names or null for auto-routing
      });

      return {
        content: response.data.content,
        agent: response.data.agent,
        sessionId: response.data.session_id,
        timestamp: new Date().toISOString(),
        metadata: response.data.metadata || {}
      };
    } catch (error) {
      throw new Error(error.response?.data?.detail || 'Failed to send message');
    }
  }

  /**
   * Send message to group chat
   */
  async sendGroupChatMessage(message, sessionId = null, config = null) {
    try {
      const response = await this.api.post('/group-chat', {
        message,
        session_id: sessionId,
        config: config
      });

      return {
        content: response.data.content,
        participants: response.data.participants || [],
        speaker: response.data.speaker,
        sessionId: response.data.session_id,
        timestamp: new Date().toISOString(),
        turn_count: response.data.turn_count,
        conversation_active: response.data.conversation_active
      };
    } catch (error) {
      throw new Error(error.response?.data?.detail || 'Failed to send group chat message');
    }
  }

  /**
   * Create group chat from template
   */
  async createGroupChatFromTemplate(templateName) {
    try {
      const response = await this.api.post('/group-chat/from-template', {
        template_name: templateName
      });

      return {
        sessionId: response.data.session_id,
        templateName: response.data.template_name,
        name: response.data.name,
        description: response.data.description,
        participants: response.data.participants,
        status: response.data.status
      };
    } catch (error) {
      throw new Error(error.response?.data?.detail || 'Failed to create group chat from template');
    }
  }

  /**
   * Get available agents
   */
  async getAvailableAgents() {
    try {
      const response = await this.api.get('/agents');
      return response.data.agents || [];
    } catch (error) {
      console.error('Failed to get agents:', error);
      // Return default agents as fallback
      return [
        { id: 'generic', name: 'Generic Assistant', description: 'General purpose AI assistant' },
        { id: 'people_lookup', name: 'People Lookup', description: 'Find information about people' },
        { id: 'knowledge_finder', name: 'Knowledge Finder', description: 'Search knowledge base' },
        { id: 'bedrock_agent', name: 'Bedrock Agent', description: 'AWS Bedrock powered agent' }
      ];
    }
  }

  /**
   * Get available group chat templates
   */
  async getGroupChatTemplates() {
    try {
      const response = await this.api.get('/group-chat/templates');
      return response.data.templates || [];
    } catch (error) {
      console.error('Failed to get group chat templates:', error);
      return [];
    }
  }

  /**
   * Get active group chats
   */
  async getActiveGroupChats() {
    try {
      const response = await this.api.get('/group-chats');
      return response.data.group_chats || [];
    } catch (error) {
      console.error('Failed to get active group chats:', error);
      return [];
    }
  }

  /**
   * Reset chat session
   */
  async resetSession(sessionId) {
    try {
      const response = await this.api.post('/reset', {
        session_id: sessionId
      });
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.detail || 'Failed to reset session');
    }
  }

  /**
   * Reset group chat session
   */
  async resetGroupChat(sessionId) {
    try {
      const response = await this.api.post(`/group-chat/${sessionId}/reset`);
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.detail || 'Failed to reset group chat');
    }
  }

  /**
   * Delete group chat session
   */
  async deleteGroupChat(sessionId) {
    try {
      const response = await this.api.delete(`/group-chat/${sessionId}`);
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.detail || 'Failed to delete group chat');
    }
  }

  /**
   * Get health status
   */
  async getHealth() {
    try {
      const response = await this.api.get('/health');
      return response.data;
    } catch (error) {
      return { status: 'error', message: error.message };
    }
  }

  /**
   * Stream chat response (for future implementation)
   */
  async streamMessage(message, sessionId = null, agents = null, onChunk = null) {
    // This would be implemented for streaming responses
    // For now, we'll use the regular sendMessage method
    return this.sendMessage(message, sessionId, agents);
  }
}

export default ChatService; 