# Implementation patterns — adapt to the existing codebase

Read nearby form components, current Bootstrap 5 markup, local style tokens, and project JS utilities before adding CSS. This example demonstrates density, **not a requirement** to add a new global class or replace any working components.

```html
<form class="crm-form-compact" dir="rtl">
  <div class="crm-form-compact__grid">
    <div class="crm-form-compact__field">
      <label for="annualSales">فروش ریالی</label>
      <div class="crm-form-compact__control">
        <input id="annualSales" type="text" inputmode="decimal" placeholder="مقدار را وارد کنید">
        <span class="crm-form-compact__suffix">ریال</span>
      </div>
      <span class="field-validation" aria-live="polite"></span>
    </div>
    <div class="crm-form-compact__field">
      <label for="annualCustomers">تعداد مشتری جدید</label>
      <div class="crm-form-compact__control">
        <input id="annualCustomers" type="number" min="0" placeholder="مقدار را وارد کنید">
        <span class="crm-form-compact__suffix">نفر</span>
      </div>
    </div>
  </div>
  <div class="crm-form-compact__actions">
    <button type="button" class="crm-form-compact__button">انصراف</button>
    <button type="submit" class="crm-form-compact__button crm-form-compact__button--primary">ثبت</button>
  </div>
</form>
```

```css
/* Scope styles to this form — do not alter all inputs or all Bootstrap buttons. */
.crm-form-compact__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px 12px;
}
.crm-form-compact__field { display: flex; flex-direction: column; min-width: 0; gap: 5px; }
.crm-form-compact__field label { font-size: 13px; }
.crm-form-compact__control { display: flex; align-items: center; gap: 8px; min-width: 0; }
.crm-form-compact__control input {
  min-width: 0;
  flex: 1;
  height: 36px;
  padding: 6px 10px;
  border: 1px solid var(--form-border-color, #d9dee7);
  border-radius: 8px;
  background: transparent;
  font: inherit;
}
.crm-form-compact__suffix { flex: none; font-size: 12px; opacity: .75; }
.crm-form-compact__actions { display: flex; justify-content: flex-start; gap: 8px; margin-top: 14px; }
.crm-form-compact__button { min-height: 34px; padding: 6px 12px; border-radius: 8px; }
@media (max-width: 640px) {
  .crm-form-compact__grid { grid-template-columns: 1fr; }
  .crm-form-compact__control input { height: 42px; font-size: 16px; }
  .crm-form-compact__button { min-height: 42px; }
}
```

Practical notes:

- If an existing reusable input/button class already works, use it instead of adding the example classes.
- Do not remove validation attributes, antiforgery tokens, form event listeners, or shared helpers.
- Keep border color, focus behavior, disabled styles, and action colors consistent with project tokens/components.
- Do not use `!important` as the first fix for oversize controls; find the original source of padding/height.
- Recheck browser zoom and Persian font rendering; numerals must not be vertically clipped.
