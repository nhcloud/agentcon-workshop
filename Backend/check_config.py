"""Simple validation script for group chat configuration."""

import os
import sys
from pathlib import Path

def check_config_file(config_path):
    """Check what templates are in a config file."""
    print(f"\nChecking: {config_path}")
    
    if not config_path.exists():
        print("❌ Config file not found")
        return []
        
    try:
        import yaml
        with open(config_path, 'r', encoding='utf-8') as f:
            data = yaml.safe_load(f)
        
        group_chats = data.get('group_chats', {})
        templates = group_chats.get('templates', {})
        
        print(f"✅ Found {len(templates)} templates:")
        for name, config in templates.items():
            description = config.get('description', 'No description')
            participant_count = len(config.get('participants', []))
            print(f"   • {name}: {description} ({participant_count} participants)")
            
        return list(templates.keys())
        
    except Exception as e:
        print(f"❌ Error reading config: {e}")
        return []

def main():
    print("Group Chat Configuration Check")
    print("=" * 50)
    
    # Check both config files
    backend_dir = Path(__file__).parent
    sk_config = backend_dir / "sk_modern" / "config.yml"
    lc_config = backend_dir / "lc_modern" / "config.yml"
    
    sk_templates = check_config_file(sk_config)
    lc_templates = check_config_file(lc_config)
    
    print(f"\n" + "=" * 50)
    print("Summary:")
    print(f"SK templates: {len(sk_templates)}")
    print(f"LC templates: {len(lc_templates)}")
    
    if set(sk_templates) == set(lc_templates):
        print("✅ Templates are consistent between frameworks")
    else:
        print("⚠️  Template mismatch detected")
        print(f"   SK only: {set(sk_templates) - set(lc_templates)}")
        print(f"   LC only: {set(lc_templates) - set(sk_templates)}")

if __name__ == "__main__":
    main()