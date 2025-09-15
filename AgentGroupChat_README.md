# Agent Group Chat Implementations

This document describes the AgentGroupChat implementations for both Semantic Kernel and LangChain frameworks in the agents workshop.

## Overview

Agent Group Chat allows multiple AI agents to participate in collaborative conversations, where agents can take turns responding to messages based on their expertise and roles. Each implementation provides similar functionality but leverages the specific capabilities of their respective frameworks.

## Implementations

### 1. Semantic Kernel AgentGroupChat (`python_semantic_kernel/agents/agent_group_chat.py`)

**Features:**
- Multi-agent conversations using Semantic Kernel's ChatCompletionAgent
- Role-based participant management (Facilitator, Participant, Observer)
- Automatic speaker selection with priority system
- Conversation termination controls
- Turn limits and consecutive turn management
- Integration with Azure OpenAI

**Key Classes:**
- `SemanticKernelAgentGroupChat`: Main group chat orchestrator
- `GroupChatConfig`: Configuration for group chat behavior
- `GroupChatParticipant`: Wrapper for individual agents with metadata
- `GroupChatRole`: Enum defining agent roles

### 2. LangChain AgentGroupChat (`langchain/agents/agent_group_chat.py`)

**Features:**
- Multi-agent conversations using LangChain Azure AI Chat Completions
- Intelligent speaker selection using LLM-based routing
- AI-powered conversation summarization
- Enhanced conversation context management
- Role-based system message generation
- Integration with Azure Inference endpoints

**Key Classes:**
- `LangChainAgentGroupChat`: Main group chat orchestrator
- `GroupChatConfig`: Configuration for group chat behavior (shared with SK)
- `LangChainAgent`: Agent wrapper with LangChain-specific capabilities
- `GroupChatRole`: Enum defining agent roles (shared with SK)

## Usage Examples

### Basic Setup

#### Semantic Kernel
```python
from agents.agent_group_chat import (
    SemanticKernelAgentGroupChat, 
    GroupChatConfig, 
    GroupChatRole
)

# Create configuration
config = GroupChatConfig(
    name="Product Planning",
    max_turns=6,
    auto_select_speaker=True
)

# Initialize group chat
group_chat = SemanticKernelAgentGroupChat(config)
await group_chat.initialize()

# Add participants
await group_chat.add_participant(
    name="ProductManager",
    instructions="You are a product manager focused on user needs.",
    role=GroupChatRole.FACILITATOR,
    priority=3
)

# Start conversation
responses = await group_chat.send_message(
    "What features should we prioritize?", 
    sender="CEO"
)
```

#### LangChain
```python
from agents.agent_group_chat import (
    LangChainAgentGroupChat, 
    GroupChatConfig, 
    GroupChatRole
)

# Create configuration
config = GroupChatConfig(
    name="Technical Review",
    max_turns=5,
    auto_select_speaker=True
)

# Initialize group chat
group_chat = LangChainAgentGroupChat(config)
await group_chat.initialize()

# Add participants
await group_chat.add_participant(
    name="Architect",
    instructions="You are a software architect focused on system design.",
    role=GroupChatRole.PARTICIPANT,
    priority=2
)

# Start conversation with intelligent speaker selection
responses = await group_chat.send_message(
    "How should we scale our authentication system?", 
    sender="CTO"
)

# Get AI-generated summary
summary = await group_chat.generate_conversation_summary()
```

## Configuration Options

### GroupChatConfig Parameters

- `name`: Name of the group chat session
- `description`: Optional description of the chat purpose
- `max_turns`: Maximum number of conversation turns (default: 10)
- `enable_termination_keyword`: Enable keyword-based termination (default: True)
- `termination_keyword`: Keyword to end conversation (default: "TERMINATE")
- `require_facilitator`: Require a facilitator role (default: True)
- `auto_select_speaker`: Enable automatic speaker selection (default: True)

### Participant Options

- `name`: Unique name for the agent
- `instructions`: System prompt defining the agent's behavior and expertise
- `role`: Agent role (FACILITATOR, PARTICIPANT, OBSERVER)
- `can_initiate`: Whether the agent can start conversations (default: True)
- `max_consecutive_turns`: Maximum consecutive turns for the agent (default: 3)
- `priority`: Speaker selection priority (higher = more priority, default: 1)

## Environment Variables

### For Semantic Kernel (semantic_kernel)
```bash
AZURE_OPENAI_ENDPOINT=https://your-openai-resource.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT=your-deployment-name
AZURE_OPENAI_KEY=your-api-key
```

### For LangChain (langchain)
```bash
AZURE_INFERENCE_ENDPOINT=https://your-inference-endpoint.com/
AZURE_INFERENCE_CREDENTIAL=your-credential-or-key
GENERIC_MODEL=gpt-4o-mini
```

## Advanced Features

### LangChain-Specific Features

1. **Intelligent Speaker Selection**: Uses an LLM to select the most appropriate speaker based on conversation context and agent expertise.

2. **AI-Powered Summaries**: Generates comprehensive conversation summaries using AI.

3. **Enhanced Context Management**: Maintains conversation context for better agent responses.

### Semantic Kernel-Specific Features

1. **Native Agent Integration**: Direct integration with Semantic Kernel's agent framework.

2. **Priority-Based Selection**: Simple but effective priority-based speaker selection.

3. **Conversation State Management**: Detailed tracking of conversation state and turn management.

## Running Examples

### Semantic Kernel
```bash
cd backend/semantic_kernel
python example_group_chat.py
```

### LangChain
```bash
cd backend/langchain
python example_group_chat.py
```

## Testing

Run the validation scripts to verify the implementations:

```bash
# Semantic Kernel
cd backend/semantic_kernel
python validate_structure.py

# LangChain
cd backend/langchain
python test_group_chat.py
```

## Key Differences

| Feature | Semantic Kernel | LangChain |
|---------|----------------|-----------|
| Speaker Selection | Priority-based rotation | AI-powered intelligent selection |
| Conversation Summary | Basic text concatenation | AI-generated summaries |
| Agent Integration | Native SK ChatCompletionAgent | Custom wrapper around LangChain models |
| Context Management | SK ChatHistory | Custom message history |
| Configuration | Azure OpenAI specific | Azure Inference endpoint |

## Error Handling

Both implementations include comprehensive error handling:
- Graceful degradation when agents fail
- Automatic conversation termination on errors
- Resource cleanup in finally blocks
- Detailed logging for debugging

## Best Practices

1. **Agent Design**: Create agents with clear, specific roles and instructions
2. **Turn Limits**: Set appropriate max_turns to prevent infinite conversations
3. **Priority Setting**: Use priorities to ensure important agents speak when needed
4. **Error Handling**: Always use try/catch blocks and cleanup resources
5. **Environment Variables**: Keep credentials secure and use environment variables

## Future Enhancements

Potential improvements for both implementations:
- Multi-language support
- Persistent conversation storage
- Advanced routing algorithms
- Integration with external tools
- Performance metrics and analytics
- Custom termination conditions
- Agent learning and adaptation
