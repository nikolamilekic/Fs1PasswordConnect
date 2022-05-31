Feature: Injection

Background:
    Given vault with id "v1" and title "Vault 1"
    And item with id "i1" and title "Item 1" in vault "v1" with fields
        | Id | Label  | Value  |
        | f1 | label1 | value1 |
        | f2 | label2 | value2 |
        | f3 | label3 | value3 |
        | f4 | label4 | value4 |
        | f5 | label5 | value5 |
        | f6 | label6 | value6 |
        | f7 | label7 | value7 |
        | f8 | label8 | value8 |

Scenario: Inject in 8 different flavours
    When the user runs inject with the following text
    """
    {{ op://Vault 1/Item 1/label1 }}
    {{ op://Vault 1/Item 1/f2 }}
    {{ op://Vault 1/i1/label3 }}
    {{ op://Vault 1/i1/f4 }}
    {{ op://v1/Item 1/label5 }}
    {{ op://v1/Item 1/f6 }}
    {{ op://v1/i1/label7 }}
    {{ op://v1/i1/f8 }}
    """
    Then the result should be
    """
    value1
    value2
    value3
    value4
    value5
    value6
    value7
    value8
    """

Scenario: Inject with replacement strings in 8 different flavours
    When the user runs inject with replacements on the following text
    """
    {{ op://Vault 1/Item 1/label1 }}
    {{ op://Vault 1/Item 1/f2 }}
    {{ op://Vault 1/i1/label3 }}
    {{ op://Vault 1/i1/f4 }}
    {{ op://v1/Item 1/label5 }}
    {{ op://v1/Item 1/f6 }}
    {{ op://v1/i1/label7 }}
    {{ op://v1/i1/f8 }}
    """
    Then the returned replacements should be should be
        | Replacement                      | Value  |
        | {{ op://Vault 1/Item 1/label1 }} | value1 |
        | {{ op://Vault 1/Item 1/f2 }}     | value2 |
        | {{ op://Vault 1/i1/label3 }}     | value3 |
        | {{ op://Vault 1/i1/f4 }}         | value4 |
        | {{ op://v1/Item 1/label5 }}      | value5 |
        | {{ op://v1/Item 1/f6 }}          | value6 |
        | {{ op://v1/i1/label7 }}          | value7 |
        | {{ op://v1/i1/f8 }}              | value8 |
