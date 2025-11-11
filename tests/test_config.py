"""
Tests for the config module.
"""

import os
import tempfile

from kick_presence.config import Config


def test_config_default_values():
    """Test that config provides sensible defaults."""
    with tempfile.TemporaryDirectory() as temp_dir:
        config_path = os.path.join(temp_dir, "test_config.json")
        config = Config(config_path)

        # Test default values exist
        assert config.get("kick.username") == ""
        assert config.get("kick.check_interval") == 30
        assert config.get("kick.enable_notifications") is True
        assert config.get("discord.client_id") == ""
        assert config.get("discord.enable_rich_presence") is True
        assert config.get("gui.theme") == "dark"
        assert config.get("gui.start_minimized") is False
        assert config.get("gui.minimize_to_tray") is True


def test_config_set_and_get():
    """Test setting and getting config values."""
    with tempfile.TemporaryDirectory() as temp_dir:
        config_path = os.path.join(temp_dir, "test_config.json")
        config = Config(config_path)

        # Test setting values
        config.set("kick.username", "testuser")
        config.set("kick.check_interval", 60)

        # Test getting values
        assert config.get("kick.username") == "testuser"
        assert config.get("kick.check_interval") == 60

        # Test default value for non-existent key
        assert config.get("nonexistent.key", "default") == "default"


def test_config_persistence():
    """Test that config is persisted to file."""
    with tempfile.TemporaryDirectory() as temp_dir:
        config_path = os.path.join(temp_dir, "test_config.json")

        # Create first config instance and modify values
        config1 = Config(config_path)
        config1.set("kick.username", "persisted_user")
        config1.save_config()

        # Create second config instance and verify values are loaded
        config2 = Config(config_path)
        assert config2.get("kick.username") == "persisted_user"


def test_config_handles_invalid_json():
    """Test that config handles invalid JSON gracefully."""
    with tempfile.TemporaryDirectory() as temp_dir:
        config_path = os.path.join(temp_dir, "invalid_config.json")

        # Write invalid JSON
        with open(config_path, "w") as f:
            f.write("{ invalid json }")

        # Should fall back to defaults
        config = Config(config_path)
        assert config.get("kick.username") == ""
