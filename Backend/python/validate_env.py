"""Environment configuration validation script."""

import os
import sys
from pathlib import Path
from typing import Dict, List, Optional

def validate_env_file(framework: str, env_path: Path) -> Dict[str, any]:
    """Validate environment configuration for a framework."""
    
    print(f"\n🔍 Validating {framework.upper()} Environment Configuration")
    print("=" * 60)
    
    if not env_path.exists():
        print(f"❌ Environment file not found: {env_path}")
        return {"valid": False, "errors": ["File not found"]}
    
    # Load environment variables from file
    env_vars = {}
    try:
        with open(env_path, 'r', encoding='utf-8') as f:
            for line_num, line in enumerate(f, 1):
                line = line.strip()
                if line and not line.startswith('#') and '=' in line:
                    key, value = line.split('=', 1)
                    env_vars[key.strip()] = value.strip()
    except Exception as e:
        print(f"❌ Error reading file: {e}")
        return {"valid": False, "errors": [f"File read error: {e}"]}
    
    # Define required variables per framework
    common_required = [
        "ENVIRONMENT",
        "HOST", 
        "PORT",
        "FRONTEND_URL"
    ]
    
    azure_openai_required = [
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_API_KEY",
        "AZURE_OPENAI_DEPLOYMENT_NAME"
    ]
    
    azure_foundry_required = [
        "AZURE_AI_PROJECT_ENDPOINT",
        "AZURE_FOUNDRY_PEOPLE_AGENT_ID",
        "AZURE_FOUNDRY_KNOWLEDGE_AGENT_ID"
    ]
    
    # Framework-specific requirements
    if framework == "sk":
        required_vars = common_required + azure_openai_required + azure_foundry_required
        recommended_vars = ["AZURE_SPEECH_API_KEY", "AZURE_SPEECH_REGION"]
    else:  # lc
        lc_required = [
            "AZURE_INFERENCE_ENDPOINT",
            "AZURE_INFERENCE_CREDENTIAL"
        ]
        required_vars = common_required + lc_required + azure_foundry_required
        recommended_vars = ["AZURE_SPEECH_API_KEY", "LANGCHAIN_TRACING_V2"]
    
    # Validation results
    results = {
        "valid": True,
        "errors": [],
        "warnings": [],
        "info": []
    }
    
    # Check required variables
    missing_required = []
    for var in required_vars:
        if var not in env_vars or not env_vars[var]:
            missing_required.append(var)
            results["valid"] = False
    
    if missing_required:
        results["errors"].append(f"Missing required variables: {', '.join(missing_required)}")
        print(f"❌ Missing required variables: {', '.join(missing_required)}")
    else:
        print("✅ All required variables present")
    
    # Check recommended variables
    missing_recommended = []
    for var in recommended_vars:
        if var not in env_vars or not env_vars[var]:
            missing_recommended.append(var)
    
    if missing_recommended:
        results["warnings"].append(f"Missing recommended variables: {', '.join(missing_recommended)}")
        print(f"⚠️  Missing recommended variables: {', '.join(missing_recommended)}")
    else:
        print("✅ All recommended variables present")
    
    # Validate specific values
    print("\n📋 Configuration Summary:")
    
    # Environment
    env_mode = env_vars.get("ENVIRONMENT", "unknown")
    if env_mode in ["development", "staging", "production"]:
        print(f"✅ Environment: {env_mode}")
    else:
        print(f"⚠️  Environment: {env_mode} (should be development/staging/production)")
        results["warnings"].append(f"Unusual environment mode: {env_mode}")
    
    # Port
    port = env_vars.get("PORT", "unknown")
    expected_port = "8001" if framework == "sk" else "8000"
    if port == expected_port:
        print(f"✅ Port: {port} (correct for {framework.upper()})")
    else:
        print(f"⚠️  Port: {port} (expected {expected_port} for {framework.upper()})")
        results["warnings"].append(f"Unexpected port: {port}")
    
    # Azure endpoints
    azure_endpoint = env_vars.get("AZURE_OPENAI_ENDPOINT", "")
    if azure_endpoint:
        if azure_endpoint.startswith("https://") and "openai.azure.com" in azure_endpoint:
            print("✅ Azure OpenAI endpoint format valid")
        else:
            print("⚠️  Azure OpenAI endpoint format may be incorrect")
            results["warnings"].append("Azure OpenAI endpoint format issue")
    
    # Framework-specific checks
    if framework == "lc":
        inference_endpoint = env_vars.get("AZURE_INFERENCE_ENDPOINT", "")
        if inference_endpoint and "cognitiveservices.azure.com" in inference_endpoint:
            print("✅ Azure Inference endpoint format valid")
        elif inference_endpoint:
            print("⚠️  Azure Inference endpoint format may be incorrect")
            results["warnings"].append("Azure Inference endpoint format issue")
    
    # Security checks
    print("\n🔒 Security Analysis:")
    
    # Check for placeholder values
    placeholders = ["your_", "YOUR_", "your-", "example", "placeholder"]
    for var, value in env_vars.items():
        if any(placeholder in value.lower() for placeholder in placeholders):
            print(f"⚠️  {var} appears to contain placeholder value")
            results["warnings"].append(f"{var} has placeholder value")
    
    # Check CORS settings
    cors_origins = env_vars.get("CORS_ALLOW_ORIGINS", "")
    if "localhost" in cors_origins and env_mode == "production":
        print("⚠️  CORS allows localhost in production environment")
        results["warnings"].append("CORS localhost in production")
    elif cors_origins:
        print("✅ CORS settings configured")
    
    # Summary
    print(f"\n📊 Summary for {framework.upper()}:")
    print(f"   • Total variables: {len(env_vars)}")
    print(f"   • Required variables: {len(required_vars) - len(missing_required)}/{len(required_vars)}")
    print(f"   • Recommended variables: {len(recommended_vars) - len(missing_recommended)}/{len(recommended_vars)}")
    print(f"   • Errors: {len(results['errors'])}")
    print(f"   • Warnings: {len(results['warnings'])}")
    
    return results

def main():
    """Main validation function."""
    print("🔧 Environment Configuration Validator")
    print("=" * 60)
    
    backend_dir = Path(__file__).parent
    
    # Validate both frameworks
    sk_results = validate_env_file("sk", backend_dir / "sk_modern" / ".env")
    lc_results = validate_env_file("lc", backend_dir / "lc_modern" / ".env")
    
    # Overall summary
    print("\n" + "=" * 60)
    print("🎯 Overall Validation Results")
    print("=" * 60)
    
    sk_status = "✅ VALID" if sk_results["valid"] else "❌ INVALID"
    lc_status = "✅ VALID" if lc_results["valid"] else "❌ INVALID"
    
    print(f"Semantic Kernel: {sk_status}")
    print(f"LangChain: {lc_status}")
    
    if sk_results["valid"] and lc_results["valid"]:
        print("\n🎉 All configurations are valid!")
        print("\nNext steps:")
        print("1. Start SK API: cd sk_modern && python main.py")
        print("2. Start LC API: cd lc_modern && python main.py")
        print("3. Test endpoints with provided examples")
        return 0
    else:
        print("\n❌ Configuration issues found. Please review and fix:")
        for framework, results in [("SK", sk_results), ("LC", lc_results)]:
            if results["errors"]:
                print(f"\n{framework} Errors:")
                for error in results["errors"]:
                    print(f"  • {error}")
        return 1

if __name__ == "__main__":
    sys.exit(main())