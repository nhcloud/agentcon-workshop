"""Test script to validate group chat configuration loading."""

import sys
import os
from pathlib import Path

# Add current directory to path for imports
sys.path.insert(0, str(Path(__file__).parent))

def test_sk_config():
    """Test Semantic Kernel configuration loading."""
    print("=== Testing SK Modern Configuration ===")
    
    try:
        # Change to SK directory
        original_dir = os.getcwd()
        sk_dir = Path(__file__).parent / "sk_modern"
        os.chdir(sk_dir)
        
        from group_chat_config import get_config_loader
        
        config_loader = get_config_loader()
        templates = config_loader.list_available_templates()
        
        print(f"✓ SK config loaded successfully")
        print(f"✓ Found {len(templates)} templates: {', '.join(templates)}")
        
        # Test template loading
        if templates:
            template_name = templates[0]
            template_info = config_loader.get_template_info(template_name)
            participants = config_loader.get_template_participants(template_name)
            
            print(f"✓ Template '{template_name}' loaded: {template_info['participants_count']} participants")
            
        return True
        
    except Exception as e:
        print(f"✗ SK config error: {e}")
        return False
        
    finally:
        os.chdir(original_dir)


def test_lc_config():
    """Test LangChain configuration loading."""
    print("\n=== Testing LC Modern Configuration ===")
    
    try:
        # Change to LC directory
        original_dir = os.getcwd()
        lc_dir = Path(__file__).parent / "lc_modern"
        os.chdir(lc_dir)
        
        from group_chat_config import get_config_loader
        
        config_loader = get_config_loader()
        templates = config_loader.list_available_templates()
        
        print(f"✓ LC config loaded successfully")
        print(f"✓ Found {len(templates)} templates: {', '.join(templates)}")
        
        # Test template loading
        if templates:
            template_name = templates[0]
            template_info = config_loader.get_template_info(template_name)
            participants = config_loader.get_template_participants(template_name)
            
            print(f"✓ Template '{template_name}' loaded: {template_info['participants_count']} participants")
            
        return True
        
    except Exception as e:
        print(f"✗ LC config error: {e}")
        return False
        
    finally:
        os.chdir(original_dir)


def test_config_consistency():
    """Test that both frameworks have consistent templates."""
    print("\n=== Testing Configuration Consistency ===")
    
    try:
        # Get SK templates
        original_dir = os.getcwd()
        sk_dir = Path(__file__).parent / "sk_modern"
        os.chdir(sk_dir)
        
        from group_chat_config import get_config_loader as get_sk_loader
        sk_loader = get_sk_loader()
        sk_templates = set(sk_loader.list_available_templates())
        
        os.chdir(original_dir)
        
        # Get LC templates
        lc_dir = Path(__file__).parent / "lc_modern"
        os.chdir(lc_dir)
        
        from group_chat_config import get_config_loader as get_lc_loader
        lc_loader = get_lc_loader()
        lc_templates = set(lc_loader.list_available_templates())
        
        # Compare
        if sk_templates == lc_templates:
            print(f"✓ Both frameworks have consistent templates: {len(sk_templates)} templates")
            print(f"  Templates: {', '.join(sorted(sk_templates))}")
        else:
            print(f"⚠ Template mismatch:")
            print(f"  SK only: {sk_templates - lc_templates}")
            print(f"  LC only: {lc_templates - sk_templates}")
            
        return True
        
    except Exception as e:
        print(f"✗ Consistency check error: {e}")
        return False
        
    finally:
        os.chdir(original_dir)


if __name__ == "__main__":
    print("Group Chat Configuration Validation")
    print("=" * 50)
    
    sk_ok = test_sk_config()
    lc_ok = test_lc_config()
    consistency_ok = test_config_consistency()
    
    print("\n" + "=" * 50)
    if sk_ok and lc_ok and consistency_ok:
        print("✓ All tests passed! Configuration is working correctly.")
        print("\nNext steps:")
        print("1. Run: cd sk_modern && python example_template_usage.py")
        print("2. Run: cd lc_modern && python example_template_usage.py")
        print("3. Start APIs: python main.py in each directory")
    else:
        print("✗ Some tests failed. Please check the configuration files.")
        sys.exit(1)