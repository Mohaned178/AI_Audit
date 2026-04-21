# AI Usage Guard — Project Overview

## Executive Summary

AI Usage Guard is a backend-first SaaS platform that helps teams monitor and govern how AI tools are used inside their organization.

The main problem it solves is lack of visibility. In many teams, people use tools like ChatGPT, Claude, Gemini, or internal AI workflows without any central tracking. That creates issues around cost, policy violations, risky data sharing, and unapproved tool usage.

This project provides a central system that receives AI usage events, stores them, evaluates them against simple rules, and shows the results through dashboards, alerts, and reports.

The goal of the project is not to build an AI model. The goal is to build the management and monitoring layer around AI usage.

This makes the project strong for a backend portfolio because it includes real SaaS concerns such as:

* multi-tenant architecture
* authentication and authorization
* event ingestion
* rule-based risk detection
* background jobs
* notifications
* reporting
* audit logging
* usage limits and billing-ready design

The public surface should stay simple. The main value of the project is in the backend design, APIs, workflows, and system behavior.

---

## Product Vision

Build a practical SaaS product that gives organizations visibility into AI tool usage, basic governance controls, and operational reporting.

The product should answer questions like:

* Who is using AI tools inside the workspace?
* Which tools are being used?
* How often are they being used?
* What is the estimated usage cost?
* Did a risky event happen?
* Are users using approved or unapproved tools?
* Has a team exceeded its limits or budget?

---

## Target Users

### Primary Users

* small companies
* startups
* engineering teams
* operations/admin teams

### Secondary Users

* freelancers managing team members
* agencies
* internal IT or compliance teams in small organizations

---

## Core Product Idea

The system receives AI usage events such as:

* prompt submitted
* file uploaded
* tool used
* cost recorded
* model invoked

It then:

1. validates and stores the event
2. associates it with a workspace and user
3. evaluates it against risk and policy rules
4. creates alerts if needed
5. exposes summaries and reports to admins

---

## High-Level Scope

### In Scope

* multi-tenant SaaS foundation
* AI usage event tracking
* simple risk detection rules
* admin dashboard APIs
* reporting APIs
* notifications and background jobs
* audit logs
* usage limits

### Out of Scope for Early Versions

* advanced machine learning risk classification
* direct integrations with many third-party AI tools
* complex client presentation design system
* enterprise-grade compliance implementation
* deep NLP analysis of prompts

---

## Suggested Delivery Strategy

The project should be built in phases. Each phase should have its own Spec Kit workflow and own clear scope.

Each phase should answer three questions:

1. what problem this phase solves
2. what new capability is delivered
3. what is intentionally left out

---

# Project Phases

## Phase 0 — Constitution and Project Rules

### Goal

Define the technical and architectural rules that guide all future specs and implementation decisions.

### Why This Phase Exists

Without shared rules, later specs become inconsistent and bloated. This phase creates the base constraints for the project.

### Main Output

A project constitution covering:

* backend-first mindset
* API-first design
* modular monolith structure
* tenant isolation rules
* security-first defaults
* auditability for sensitive actions
* simple API and integration policy
* testing expectations
* naming and documentation standards

### Out of Scope

* business features
* UI implementation
* event processing

---

## Phase 1 — Core SaaS Foundation

### Goal

Build the base SaaS structure required for all future features.

### Problem Solved

The system needs identity, workspaces, tenant boundaries, and access control before it can safely manage AI usage data.

### Main Features

* workspace creation
* user registration/login
* authentication
* role-based access control
* roles such as owner, admin, member
* tenant-aware data access
* basic user management
* simple dashboard placeholder

### Expected Outcome

After this phase, the application behaves like a real SaaS platform with isolated workspaces and authenticated users.

### Out of Scope

* AI usage events
* risk detection
* reports
* notifications

---

## Phase 2 — AI Usage Event Ingestion

### Goal

Allow the platform to receive and store AI usage events.

### Problem Solved

The product cannot provide visibility or governance unless it can collect structured usage events from users, tools, or internal systems.

### Main Features

* event ingestion endpoint(s)
* payload validation
* event persistence
* event types definition
* event history retrieval
* event filtering basics

### Example Event Types

* prompt_submitted
* file_uploaded
* tool_used
* usage_recorded
* model_called

### Expected Outcome

After this phase, the system becomes capable of collecting the raw data that powers the product.

### Out of Scope

* advanced risk scoring
* notification workflows
* billing logic

---

## Phase 3 — Risk Detection Engine

### Goal

Turn raw events into useful governance signals.

### Problem Solved

Raw event storage alone is not enough. Admins need to know when something risky or policy-related happens.

### Main Features

* simple rule engine
* policy/risk checks
* alert creation
* severity levels
* risk reason tracking

### Example Rules

* detect email patterns
* detect phone number patterns
* detect file upload events
* detect unapproved tool usage
* detect threshold/budget exceedance

### Expected Outcome

After this phase, the system can automatically flag risky or suspicious AI usage activity.

### Out of Scope

* machine learning classification
* deep content understanding
* complex policy language

---

## Phase 4 — Reporting and Dashboard APIs

### Goal

Provide useful operational visibility to admins.

### Problem Solved

Admins need to understand usage trends, risk trends, and estimated costs in a simple way.

### Main Features

* usage summaries by user
* usage summaries by workspace
* alerts summary
* tool usage summary
* estimated cost summary
* date range reporting
* dashboard endpoints for simple API consumers

### Expected Outcome

After this phase, the product delivers visible business value through dashboards and summaries.

### Out of Scope

* advanced analytics
* forecasting
* custom report builder

---

## Phase 5 — Background Jobs and Notifications

### Goal

Automate recurring system work and alert delivery.

### Problem Solved

Some system actions should happen asynchronously or on schedules, not during direct API requests.

### Main Features

* scheduled summary jobs
* recurring scans/checks
* email or in-app notifications
* retry handling for failed jobs
* digest generation

### Expected Outcome

After this phase, the platform behaves more like a production SaaS with automation and operational workflows.

### Out of Scope

* multi-channel enterprise notification center
* complex workflow automation

---

## Phase 6 — Usage Limits and Billing-Ready Design

### Goal

Prepare the product for monetization and plan-based restrictions.

### Problem Solved

A SaaS product needs controlled usage and a path toward pricing tiers.

### Main Features

* plan definitions
* monthly usage limits
* per-workspace quotas
* limit enforcement basics
* overage or warning logic
* billing-ready domain design

### Expected Outcome

After this phase, the product can support free and paid plan behavior even if full payment integration comes later.

### Out of Scope

* full payment provider integration in the first iteration
* complex invoicing
* tax handling

---

## Phase 7 — Audit Logs and Hardening

### Goal

Improve trust, maintainability, and production readiness.

### Problem Solved

Operational systems need traceability, rate protection, and stronger error handling before they can be considered reliable.

### Main Features

* audit logs for important actions
* structured logging
* rate limiting
* standardized error responses
* permission review
* security hardening review

### Expected Outcome

After this phase, the project feels more mature and production-minded.

### Out of Scope

* enterprise certification work
* advanced SIEM integrations

---

# Recommended Build Order

1. Phase 0 — Constitution and Project Rules
2. Phase 1 — Core SaaS Foundation
3. Phase 2 — AI Usage Event Ingestion
4. Phase 3 — Risk Detection Engine
5. Phase 4 — Reporting and Dashboard APIs
6. Phase 5 — Background Jobs and Notifications
7. Phase 6 — Usage Limits and Billing-Ready Design
8. Phase 7 — Audit Logs and Hardening

---

# Notes for Spec Kit Usage

Each phase should go through its own Spec Kit cycle.

Recommended sequence per phase:

1. specify
2. clarify
3. plan
4. tasks
5. implement

Important rule: do not merge multiple major phases into one spec. Keep each phase narrow, testable, and reviewable.

---

# Definition of Success

This project is successful if it demonstrates:

* strong backend architecture
* clear SaaS thinking
* realistic business use case
* clean separation of responsibilities
* phased delivery with controlled scope
* enough API surface to demonstrate value without turning into a client-heavy project

It is not necessary for the first version to support many integrations or advanced AI logic. A focused and well-executed backend product is enough to make it strong for a CV and portfolio.
