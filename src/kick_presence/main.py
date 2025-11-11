"""
Main entry point for the Kick Presence application.
"""

import logging

from kick_presence.config import Config
from kick_presence.logging_config import setup_logging


def main() -> None:
    """Main entry point for the application."""
    # Setup logging
    setup_logging()
    logger = logging.getLogger(__name__)

    logger.info("Kick Presence starting...")

    # Load configuration
    config = Config()
    logger.info(f"Configuration loaded: {config}")

    # TODO: Initialize GUI, Kick monitoring, and Discord Rich Presence
    print("Kick Presence - Python desktop app for Kick monitoring")
    print("This is a placeholder. Full implementation coming soon.")

    logger.info("Kick Presence initialized successfully")


if __name__ == "__main__":
    main()
