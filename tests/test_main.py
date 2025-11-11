"""
Tests for the main module.
"""

from unittest.mock import patch

from kick_presence.main import main


def test_main_runs_without_errors():
    """Test that main function runs without errors."""
    with (
        patch("kick_presence.main.setup_logging"),
        patch("kick_presence.main.Config") as mock_config,
    ):

        mock_config.return_value = "mock_config"

        # Should not raise any exceptions
        main()

        # Verify Config was instantiated
        mock_config.assert_called_once()
