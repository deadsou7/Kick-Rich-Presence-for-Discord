# Makefile for Kick Presence Python project

.PHONY: help install install-dev test test-cov lint format clean run build

# Default target
help:
	@echo "Available commands:"
	@echo "  install      - Install runtime dependencies"
	@echo "  install-dev  - Install development dependencies"
	@echo "  test         - Run tests"
	@echo "  test-cov     - Run tests with coverage"
	@echo "  lint         - Run linting (ruff)"
	@echo "  format       - Format code (black, ruff)"
	@echo "  type-check   - Run type checking (mypy)"
	@echo "  clean        - Clean build artifacts"
	@echo "  run          - Run the application"
	@echo "  build        - Build the package"

# Install dependencies
install:
	pip install -r requirements.txt

install-dev:
	pip install -r requirements.txt -r requirements-dev.txt
	pre-commit install

# Testing
test:
	PYTHONPATH=src pytest

test-cov:
	PYTHONPATH=src pytest --cov=src/kick_presence --cov-report=html --cov-report=term-missing

# Code quality
lint:
	PYTHONPATH=src ruff check src tests

format:
	black src tests
	PYTHONPATH=src ruff check --fix src tests

type-check:
	PYTHONPATH=src mypy src

# Combined quality check
check: lint type-check test

# Clean build artifacts
clean:
	rm -rf build/
	rm -rf dist/
	rm -rf *.egg-info/
	rm -rf htmlcov/
	rm -rf .pytest_cache/
	rm -rf .mypy_cache/
	find . -type d -name __pycache__ -delete
	find . -type f -name "*.pyc" -delete

# Run the application
run:
	PYTHONPATH=src python -m kick_presence

# Build the package
build: clean
	python -m build

# Development setup
setup: install-dev
	@echo "Development environment setup complete!"
