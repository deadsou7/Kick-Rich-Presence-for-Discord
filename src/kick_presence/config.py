"""
Configuration management for Kick Presence.
"""

import json
import os
from pathlib import Path
from typing import Any


class Config:
    """Configuration manager for the application."""

    def __init__(self, config_path: str | None = None):
        """Initialize configuration.

        Args:
            config_path: Path to configuration file. If None, uses default location.
        """
        self.config_path = config_path or self._get_default_config_path()
        self.config_data = self._load_config()

    def _get_default_config_path(self) -> str:
        """Get the default configuration file path."""
        # Store config in user's home directory under .kick_presence
        home_dir = Path.home()
        config_dir = home_dir / ".kick_presence"
        config_dir.mkdir(exist_ok=True)
        return str(config_dir / "config.json")

    def _load_config(self) -> dict[str, Any]:
        """Load configuration from file."""
        default_config = {
            "kick": {"username": "", "check_interval": 30, "enable_notifications": True},  # seconds
            "discord": {"client_id": "", "enable_rich_presence": True},
            "gui": {"theme": "dark", "start_minimized": False, "minimize_to_tray": True},
            "logging": {"level": "INFO", "file": None},  # Will use default if None
        }

        if os.path.exists(self.config_path):
            try:
                with open(self.config_path, encoding="utf-8") as f:
                    loaded_config: dict[str, Any] = json.load(f)
                    # Merge with defaults
                    for key, value in default_config.items():
                        if key not in loaded_config:
                            loaded_config[key] = value
                        elif isinstance(value, dict) and isinstance(loaded_config[key], dict):
                            for sub_key, sub_value in value.items():
                                if sub_key not in loaded_config[key]:
                                    loaded_config[key][sub_key] = sub_value
                    return loaded_config
            except (OSError, json.JSONDecodeError) as e:
                print(f"Error loading config from {self.config_path}: {e}")
                return default_config
        else:
            # Create default config file
            self.save_config(default_config)
            return default_config

    def save_config(self, config_data: dict[str, Any] | None = None) -> None:
        """Save configuration to file.

        Args:
            config_data: Configuration data to save. If None, saves current config.
        """
        data_to_save = config_data or self.config_data
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                json.dump(data_to_save, f, indent=2)
        except OSError as e:
            print(f"Error saving config to {self.config_path}: {e}")

    def get(self, key: str, default: Any = None) -> Any:
        """Get configuration value.

        Args:
            key: Configuration key (supports dot notation, e.g., 'kick.username')
            default: Default value if key not found

        Returns:
            Configuration value or default
        """
        keys = key.split(".")
        value = self.config_data

        for k in keys:
            if isinstance(value, dict) and k in value:
                value = value[k]
            else:
                return default

        return value

    def set(self, key: str, value: Any) -> None:
        """Set configuration value.

        Args:
            key: Configuration key (supports dot notation, e.g., 'kick.username')
            value: Value to set
        """
        keys = key.split(".")
        config = self.config_data

        for k in keys[:-1]:
            if k not in config:
                config[k] = {}
            config = config[k]

        config[keys[-1]] = value

    def __str__(self) -> str:
        """String representation of configuration."""
        return json.dumps(self.config_data, indent=2)
