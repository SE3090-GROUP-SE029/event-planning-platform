"""Concise, constrained prompt templates for coordinator steps."""

REQUIREMENTS_ANALYSIS_PROMPT = """
You are an expert event planning analyst. Analyze the event details below.
Think through the requirements step by step, but return only the requested
analysis. Cover purpose and context, success factors, scale, stakeholders, and
initial constraints. Do not invent facts.

Event details:
{event_details}

Return a concise, cohesive analysis in plain text.
"""

SERVICE_CATEGORY_PROMPT = """
You are an event planning coordinator. Identify the generic service categories
needed for this event from the details and analysis below.
MUST NOT recommend vendors, companies, brands, or named businesses.
MUST use generic categories such as "Catering" and "Photography".

Event: {event_details}
Analysis: {analysis}

Think step by step, then return only a JSON array of category strings.
"""

BUDGET_ALLOCATION_PROMPT = """
You are an event budget planner. Allocate the total budget across the supplied
generic service categories. Every allocation MUST be positive, the total MUST
not exceed the total budget, and a 5-10% contingency MUST be included.
MUST NOT name vendors or invent costs outside the supplied budget.

Total budget: {total_budget}
Categories: {service_categories}
Guest count: {guest_count}

Return only JSON matching the requested budget allocation schema.
"""

TIMELINE_GENERATION_PROMPT = """
You are an event logistics planner. Create at least three practical planning
phases for the event date, complexity, and vendor count below. Include timing
and a useful description. MUST avoid impossible or conflicting dates.

Event date: {event_date}
Complexity: {complexity}
Vendor count: {vendor_count}

Think step by step, then return only JSON matching the timeline schema.
"""

RISK_ASSESSMENT_PROMPT = """
You are an event risk assessor. Identify realistic operational, budget, and
timeline risks from the details below. Assign Low, Medium, or High severity and
give an actionable recommendation. Do not invent risks unrelated to the input.

Event details: {event_details}
Budget: {budget}
Constraints: {constraints}

Return only JSON matching the risk schema.
"""

MISSING_REQUIREMENT_DETECTION_PROMPT = """
You are an event planning requirements analyst. Find information gaps that
could materially affect planning. Include the missing requirement and why it
matters. Do not report information already present and do not recommend vendors.

Event details: {event_details}
Requirements: {requirements}

Return only JSON matching the missing-requirements schema.
"""

COMPLETENESS_SCORING_PROMPT = """
You are a planning quality assessor. Calculate a score from 0 to 100 using
these rules: critical fields 40, event type 15, complete budget 20, timeline
15, identified risks 10. Use only the supplied information.

Information: {information}

Return only a JSON object containing an integer score and a short explanation.
"""

RATIONALE_GENERATION_PROMPT = """
You are an experienced event planner. Explain clearly why the proposed
categories, allocations, and timeline fit the event. Mention relevant
constraints and trade-offs. Do not claim facts not present in the input.

Event: {event_details}
Categories: {service_categories}
Allocations: {budget_allocation}
Timeline: {timeline}

Return a concise rationale in plain text.
"""

PLAN_GENERATION_PROMPT = """
You are a professional event planning coordinator. Create a practical,
internally consistent plan from the event details below. Think through the
constraints step by step, but return only JSON matching the supplied schema.
MUST NOT recommend named vendors or expose private reasoning.

Event details:
{event_data}

Required JSON schema:
{schema}
"""

VALIDATION_PROMPT = """
You are a meticulous event-plan validator. Check the candidate plan for
missing fields, invalid ranges, budget overflow, and contradictory timing.
Return only actionable validation errors and a pass/fail result.

Candidate plan: {plan}
"""
