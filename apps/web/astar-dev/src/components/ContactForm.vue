<script setup lang="ts">
import { ref, reactive } from 'vue';

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface FormFields {
  name: string;
  email: string;
  message: string;
  sendCopy: boolean;
  website: string; // honeypot
}

interface FieldErrors {
  name: string;
  email: string;
  message: string;
}

type SubmitStatus = 'idle' | 'submitting' | 'success' | 'error';

// ---------------------------------------------------------------------------
// State
// ---------------------------------------------------------------------------

const fields = reactive<FormFields>({
  name: '',
  email: '',
  message: '',
  sendCopy: false,
  website: '',
});

const fieldErrors = reactive<FieldErrors>({
  name: '',
  email: '',
  message: '',
});

const submitStatus = ref<SubmitStatus>('idle');
const statusMessage = ref('');

const nameRef = ref<HTMLElement | null>(null);
const emailRef = ref<HTMLElement | null>(null);
const messageRef = ref<HTMLElement | null>(null);
const statusRef = ref<HTMLElement | null>(null);

// ---------------------------------------------------------------------------
// Validation
// ---------------------------------------------------------------------------

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function validateFields(): boolean {
  fieldErrors.name = '';
  fieldErrors.email = '';
  fieldErrors.message = '';

  const name = fields.name.trim();
  const email = fields.email.trim();
  const message = fields.message.trim();

  let isValid = true;

  if (name.length === 0) {
    fieldErrors.name = 'Name is required.';
    isValid = false;
  } else if (name.length > 200) {
    fieldErrors.name = 'Name must be 200 characters or fewer.';
    isValid = false;
  }

  if (email.length === 0) {
    fieldErrors.email = 'Email is required.';
    isValid = false;
  } else if (!EMAIL_RE.test(email)) {
    fieldErrors.email = 'Please enter a valid email address.';
    isValid = false;
  }

  if (message.length === 0) {
    fieldErrors.message = 'Message is required.';
    isValid = false;
  } else if (message.length < 10) {
    fieldErrors.message = 'Message must be at least 10 characters.';
    isValid = false;
  } else if (message.length > 5000) {
    fieldErrors.message = 'Message must be 5000 characters or fewer.';
    isValid = false;
  }

  return isValid;
}

// ---------------------------------------------------------------------------
// Submit handler
// ---------------------------------------------------------------------------

async function handleSubmit(): Promise<void> {
  if (!validateFields()) {
    // Move focus to first invalid field
    if (fieldErrors.name.length > 0 && nameRef.value !== null) {
      nameRef.value.focus();
    } else if (fieldErrors.email.length > 0 && emailRef.value !== null) {
      emailRef.value.focus();
    } else if (fieldErrors.message.length > 0 && messageRef.value !== null) {
      messageRef.value.focus();
    }
    return;
  }

  submitStatus.value = 'submitting';
  statusMessage.value = '';

  try {
    const response = await fetch('/api/contact', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: fields.name.trim(),
        email: fields.email.trim(),
        message: fields.message.trim(),
        sendCopy: fields.sendCopy,
        website: fields.website,
      }),
    });

    if (response.ok) {
      submitStatus.value = 'success';
      statusMessage.value = 'Thank you for your message. We will be in touch soon.';

      // Reset form
      fields.name = '';
      fields.email = '';
      fields.message = '';
      fields.sendCopy = false;
      fields.website = '';
    } else if (response.status === 429) {
      submitStatus.value = 'error';
      statusMessage.value = 'Too many requests. Please try again in 15 minutes.';
    } else {
      const data: unknown = await response.json().catch(() => null);
      const serverMessage =
        typeof data === 'object' && data !== null && 'message' in data && typeof (data as Record<string, unknown>).message === 'string'
          ? (data as Record<string, unknown>).message as string
          : 'Something went wrong. Please try again later.';
      submitStatus.value = 'error';
      statusMessage.value = serverMessage;
    }
  } catch {
    submitStatus.value = 'error';
    statusMessage.value = 'Something went wrong. Please check your connection and try again.';
  }

  // Move focus to status message so screen readers announce it
  await new Promise<void>((resolve) => setTimeout(resolve, 50));
  if (statusRef.value !== null) {
    statusRef.value.focus();
  }
}
</script>

<template>
  <form
    class="contact-form"
    novalidate
    @submit.prevent="handleSubmit"
  >
    <!-- Honeypot field — hidden from real users, visible to bots -->
    <div class="honeypot" aria-hidden="true">
      <label for="website">Website</label>
      <input
        id="website"
        v-model="fields.website"
        type="text"
        name="website"
        tabindex="-1"
        autocomplete="off"
      />
    </div>

    <!-- Name -->
    <div class="form-group" :class="{ 'form-group--error': fieldErrors.name.length > 0 }">
      <label for="name" class="form-label">
        Name <span class="required" aria-hidden="true">*</span>
      </label>
      <input
        id="name"
        ref="nameRef"
        v-model="fields.name"
        type="text"
        class="form-input"
        autocomplete="name"
        required
        :aria-invalid="fieldErrors.name.length > 0 ? 'true' : undefined"
        :aria-describedby="fieldErrors.name.length > 0 ? 'name-error' : undefined"
        :disabled="submitStatus === 'submitting'"
      />
      <p v-if="fieldErrors.name.length > 0" id="name-error" class="field-error" role="alert">
        {{ fieldErrors.name }}
      </p>
    </div>

    <!-- Email -->
    <div class="form-group" :class="{ 'form-group--error': fieldErrors.email.length > 0 }">
      <label for="email" class="form-label">
        Email <span class="required" aria-hidden="true">*</span>
      </label>
      <input
        id="email"
        ref="emailRef"
        v-model="fields.email"
        type="email"
        class="form-input"
        autocomplete="email"
        required
        :aria-invalid="fieldErrors.email.length > 0 ? 'true' : undefined"
        :aria-describedby="fieldErrors.email.length > 0 ? 'email-error' : undefined"
        :disabled="submitStatus === 'submitting'"
      />
      <p v-if="fieldErrors.email.length > 0" id="email-error" class="field-error" role="alert">
        {{ fieldErrors.email }}
      </p>
    </div>

    <!-- Message -->
    <div class="form-group" :class="{ 'form-group--error': fieldErrors.message.length > 0 }">
      <label for="message" class="form-label">
        Message <span class="required" aria-hidden="true">*</span>
      </label>
      <textarea
        id="message"
        ref="messageRef"
        v-model="fields.message"
        class="form-textarea"
        rows="6"
        required
        :aria-invalid="fieldErrors.message.length > 0 ? 'true' : undefined"
        :aria-describedby="fieldErrors.message.length > 0 ? 'message-error' : undefined"
        :disabled="submitStatus === 'submitting'"
      ></textarea>
      <p v-if="fieldErrors.message.length > 0" id="message-error" class="field-error" role="alert">
        {{ fieldErrors.message }}
      </p>
    </div>

    <!-- Send copy checkbox -->
    <div class="form-group form-group--checkbox">
      <label class="checkbox-label">
        <input
          v-model="fields.sendCopy"
          type="checkbox"
          class="checkbox-input"
          :disabled="submitStatus === 'submitting'"
        />
        <span class="checkbox-text">Send me a copy of this message</span>
      </label>
    </div>

    <!-- Submit button -->
    <button
      type="submit"
      class="btn-submit"
      :disabled="submitStatus === 'submitting'"
    >
      {{ submitStatus === 'submitting' ? 'Sending…' : 'Send message' }}
    </button>

    <!-- Status message -->
    <div
      v-if="submitStatus === 'success' || submitStatus === 'error'"
      ref="statusRef"
      class="status-message"
      :class="{
        'status-message--success': submitStatus === 'success',
        'status-message--error': submitStatus === 'error',
      }"
      role="status"
      aria-live="polite"
      aria-atomic="true"
      tabindex="-1"
    >
      {{ statusMessage }}
    </div>
  </form>
</template>

<style scoped>
.contact-form {
  display: flex;
  flex-direction: column;
  gap: 24px;
  max-width: 640px;
}

/* Honeypot — completely off-screen and inaccessible to real users */
.honeypot {
  position: absolute;
  left: -9999px;
  width: 1px;
  height: 1px;
  overflow: hidden;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-label {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text);
  transition: color 0.25s;
}

.required {
  color: var(--accent);
  margin-left: 2px;
}

.form-input,
.form-textarea {
  padding: 10px 14px;
  background: var(--surface-raised);
  color: var(--text);
  border: 1px solid var(--border);
  border-radius: 8px;
  font-size: 0.95rem;
  font-family: inherit;
  line-height: 1.5;
  transition: border-color 0.15s, background-color 0.25s, color 0.25s;
  width: 100%;
  box-sizing: border-box;
}

.form-input:focus,
.form-textarea:focus {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
  border-color: var(--accent);
}

.form-input:disabled,
.form-textarea:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.form-group--error .form-input,
.form-group--error .form-textarea {
  border-color: #ef4444;
}

.form-textarea {
  resize: vertical;
  min-height: 140px;
}

.field-error {
  font-size: 0.82rem;
  color: #ef4444;
  margin: 0;
}

/* Checkbox group */
.form-group--checkbox {
  flex-direction: row;
  align-items: center;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  font-size: 0.9rem;
  color: var(--text);
  transition: color 0.25s;
}

.checkbox-input {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  accent-color: var(--accent);
  cursor: pointer;
}

.checkbox-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.checkbox-input:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
}

.checkbox-text {
  line-height: 1.4;
}

/* Submit button */
.btn-submit {
  align-self: flex-start;
  padding: 12px 28px;
  background: var(--btn-primary-bg);
  color: var(--btn-primary-text);
  border: 2px solid var(--btn-primary-bg);
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: transform 0.15s ease, background-color 0.25s, border-color 0.25s, color 0.25s, opacity 0.15s;
}

.btn-submit:hover:not(:disabled) {
  transform: translateY(-1px);
}

.btn-submit:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 3px;
}

.btn-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (prefers-reduced-motion: reduce) {
  .btn-submit:hover:not(:disabled) {
    transform: none;
  }
}

/* Status messages */
.status-message {
  padding: 14px 18px;
  border-radius: 8px;
  font-size: 0.9rem;
  font-weight: 500;
  line-height: 1.5;
  border: 1px solid transparent;
}

.status-message--success {
  color: var(--accent);
  background: var(--surface-raised);
  border-color: var(--border);
}

.status-message--error {
  color: #dc2626;
  background: var(--surface-raised);
  border-color: #ef4444;
}

.status-message:focus {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
  border-radius: 8px;
}
</style>
