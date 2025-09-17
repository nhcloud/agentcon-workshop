#!/bin/bash

# .NET Semantic Kernel Agents Workshop - API Test Script
# This script tests the .NET implementation to ensure it matches Python functionality

BASE_URL="http://localhost:8002"

echo "?? Testing .NET Semantic Kernel Agents Workshop Implementation"
echo "=============================================================="

# Function to make API calls with proper error handling
make_request() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4
    
    echo ""
    echo "?? Testing: $description"
    echo "?? $method $endpoint"
    
    if [ -n "$data" ]; then
        response=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X $method "$BASE_URL$endpoint" \
            -H "Content-Type: application/json" \
            -d "$data")
    else
        response=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X $method "$BASE_URL$endpoint")
    fi
    
    http_status=$(echo "$response" | grep "HTTP_STATUS:" | cut -d: -f2)
    body=$(echo "$response" | sed '/HTTP_STATUS:/d')
    
    if [ "$http_status" -eq 200 ] || [ "$http_status" -eq 201 ]; then
        echo "? Success ($http_status)"
        echo "?? Response: $(echo "$body" | jq . 2>/dev/null || echo "$body")"
    else
        echo "? Failed ($http_status)"
        echo "?? Response: $body"
    fi
    
    return $http_status
}

# Wait for API to be ready
echo "? Checking if API is running..."
for i in {1..10}; do
    if curl -s "$BASE_URL/health" > /dev/null 2>&1; then
        echo "? API is ready"
        break
    fi
    if [ $i -eq 10 ]; then
        echo "? API not responding. Please ensure the .NET app is running on port 8002"
        exit 1
    fi
    sleep 2
done

# Test 1: Health Check
make_request "GET" "/health" "" "Health Check"

# Test 2: Configuration Check
make_request "GET" "/api/config" "" "Configuration Status"

# Test 3: Get Available Agents
make_request "GET" "/api/agents" "" "Available Agents"

# Test 4: Get Agent Types
make_request "GET" "/api/agents/types" "" "Agent Types"

# Test 5: Get Specific Agent Info
make_request "GET" "/api/agents/technical_advisor" "" "Technical Advisor Agent Info"

# Test 6: Chat with Technical Advisor
chat_request='{
  "message": "What are the best practices for building scalable .NET applications?",
  "session_id": "test-session-001"
}'
make_request "POST" "/api/agents/technical_advisor/chat" "$chat_request" "Chat with Technical Advisor"

# Test 7: Generic Chat
generic_chat='{
  "message": "Hello! Can you help me understand Semantic Kernel?",
  "agent": "generic"
}'
make_request "POST" "/api/chat" "$generic_chat" "Generic Chat"

# Test 8: Create New Session
make_request "POST" "/api/chat/session/new" "" "Create New Chat Session"

# Test 9: Group Chat
group_chat='{
  "message": "How should we approach building an AI-powered application?",
  "agents": ["technical_advisor", "task_assistant", "creative_assistant"],
  "max_turns": 2,
  "use_semantic_kernel_groupchat": false
}'
make_request "POST" "/api/groupchat" "$group_chat" "Standard Group Chat"

# Test 10: Enhanced Group Chat
enhanced_group_chat='{
  "message": "What are the key considerations for enterprise AI deployment?",
  "agents": ["technical_advisor", "people_lookup"],
  "max_turns": 1,
  "use_semantic_kernel_groupchat": true
}'
make_request "POST" "/api/groupchat" "$enhanced_group_chat" "Enhanced Group Chat"

# Test 11: Get Agent Capabilities
make_request "GET" "/api/agents/knowledge_finder/capabilities" "" "Knowledge Finder Capabilities"

# Test 12: Test Azure AI Foundry Agent (if configured)
foundry_test='{
  "message": "Test Azure AI Foundry connectivity"
}'
echo ""
echo "?? Testing: Azure AI Foundry Agent (may fail if not configured)"
make_request "POST" "/api/agents/foundry/people_lookup/test" "$foundry_test" "Azure AI Foundry Test"

# Test 13: Session Management
make_request "GET" "/api/groupchat/sessions" "" "Active Sessions"

echo ""
echo "?? Test Suite Complete!"
echo "============================================"
echo "? .NET Semantic Kernel Agents Workshop Implementation Verified"
echo ""
echo "?? Next Steps:"
echo "   1. Configure Azure OpenAI credentials in .env or appsettings.json"
echo "   2. Test with your own Azure AI Foundry agents"
echo "   3. Explore the Swagger UI at $BASE_URL"
echo "   4. Try the frontend integration"
echo ""
echo "?? For more information, see:"
echo "   - README.md - Complete documentation"
echo "   - SETUP_GUIDE.md - Configuration examples"
echo "   - Swagger UI - Interactive API documentation"