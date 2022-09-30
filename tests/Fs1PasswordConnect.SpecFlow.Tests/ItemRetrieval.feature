Feature: Item Retrieval

Background:
    Given the client is configured to use host 'mock_host' and token 'jwt_token'
    And item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
        """
        {
            "id": "wepiqdxdzncjtnvmv5fegud4qy",
            "title": "Test Login",
            "tags": [
                "LastPass Import 9-19-20"
            ],
            "version": 3,
            "vault": {
                "id": "hfnjvi6aymbsnfc2xeeoheizda"
            },
            "category": "LOGIN",
            "lastEditedBy": "last edited id",
            "sections": [
                {
                    "id": "ugd62kx3xvpsrrhmwa7c64x5te",
                    "label": "Configuration"
                }
            ],
            "fields": [
                {
                    "id": "field id",
                    "type": "STRING",
                    "purpose": "purpose",
                    "label": "label",
                    "value": "value"
                },
                {
                    "id": "field id 2",
                    "type": "STRING",
                    "purpose": "purpose",
                    "label": "label1",
                    "value": "value1",
                    "section": {
                        "id": "ugd62kx3xvpsrrhmwa7c64x5te",
                        "label": "Configuration"
                    }
                }
            ],
            "urls": [
                {
                    "primary": true,
                    "href": "https://www.google.com"
                }
            ],
            "files": [
                {
                    "id": "fooaktb2fvbvdkarwm2crklmei",
                    "name": "profile.png",
                    "size": 94941,
                    "content_path": "/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy/files/fooaktb2fvbvdkarwm2crklmei/content"
                }
            ]
        }
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
        """
        [
            {
                "id": "wepiqdxdzncjtnvmv5fegud4qy",
                "title": "Test Login",
                "tags": [
                    "LastPass Import 9-19-20"
                ],
                "version": 3,
                "vault": {
                    "id": "hfnjvi6aymbsnfc2xeeoheizda"
                },
                "category": "LOGIN",
                "lastEditedBy": "last edited id",
                "urls": [
                    {
                        "primary": true,
                        "href": "https://www.google.com/"
                    }
                ]
            }
        ]
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items'
        """
        [
            {
                "id": "wepiqdxdzncjtnvmv5fegud4qy",
                "title": "Test Login",
                "tags": [
                    "LastPass Import 9-19-20"
                ],
                "version": 3,
                "vault": {
                    "id": "hfnjvi6aymbsnfc2xeeoheizda"
                },
                "category": "LOGIN",
                "lastEditedBy": "last edited id",
                "urls": [
                    {
                        "primary": true,
                        "href": "https://www.google.com/"
                    }
                ]
            }
        ]
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults?filter=title eq "Automation"'
        """
        [
            {
                "id": "hfnjvi6aymbsnfc2xeeoheizda",
                "name": "Automation",
                "attributeVersion": 1,
                "contentVersion": 14,
                "items": 3,
                "type": "USER_CREATED",
                "createdAt": "2022-01-21T19:16:59Z",
                "updatedAt": "2022-01-22T20:30:13Z"
            }
        ]
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults'
        """
        [
            {
                "id": "hfnjvi6aymbsnfc2xeeoheizda",
                "name": "Automation",
                "attributeVersion": 1,
                "contentVersion": 14,
                "items": 3,
                "type": "USER_CREATED",
                "createdAt": "2022-01-21T19:16:59Z",
                "updatedAt": "2022-01-22T20:30:13Z"
            }
        ]
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda'
        """
        {
            "id": "hfnjvi6aymbsnfc2xeeoheizda",
            "name": "Automation",
            "attributeVersion": 1,
            "contentVersion": 14,
            "items": 3,
            "type": "USER_CREATED",
            "createdAt": "2022-01-21T19:16:59Z",
            "updatedAt": "2022-01-22T20:30:13Z"
        }
        """

Scenario: Get item by ID and vault ID
    When the user requests item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by title and vault ID
    When the user requests item with title 'Test Login' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by ID and vault title
    When the user requests item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with title 'Automation'
    Then the following url should be called 'mock_host/v1/vaults?filter=title eq "Automation"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by title and vault title
    When the user requests item with title 'Test Login' in vault with title 'Automation'
    Then the following url should be called 'mock_host/v1/vaults?filter=title eq "Automation"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Getting all items in a vault by vault ID
    When the user requests all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items'
    And the client should return all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'

Scenario: Getting all items in a vault by vault title
    When the user requests all items in vault with title 'Automation'
    Then the following url should be called 'mock_host/v1/vaults?filter=title eq "Automation"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items'
    And the client should return all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'

Scenario: Getting all vaults
    When the user requests all vaults
    Then the following url should be called 'mock_host/v1/vaults'
    And the client should return all vaults

Scenario: Items are properly deserialized
    When the user requests item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with title 'Automation'
    Then the item should contain tag 'LastPass Import 9-19-20'
    And the item's title should be 'Test Login'
    And the item's id should be 'wepiqdxdzncjtnvmv5fegud4qy'
    And the item's version should be '3'
    And the item should contain field with id 'field id', label 'label' and value 'value'
    And the item should contain field with id 'field id 2', label 'label1', value 'value1', section 'Configuration' and section ID 'ugd62kx3xvpsrrhmwa7c64x5te'
    And the item should contain file with id 'fooaktb2fvbvdkarwm2crklmei', name 'profile.png', size '94941' and path '/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy/files/fooaktb2fvbvdkarwm2crklmei/content'
    And the item should contain url 'https://www.google.com'

Scenario: Vault info is properly deserialized
    When the user requests vault info for vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the vault's ID should be 'hfnjvi6aymbsnfc2xeeoheizda'
    And the vault's title should be 'Automation'
    And the vault's version should be '14'
    And the vault's created at date should be '2022-01-21T19:16:59Z'
    And the vault's updated at date should be '2022-01-22T20:30:13Z'
    And the vault's item count should be '3'

Scenario: Content path includes version (v1) so when downloading file the version should not be duplicated
    Given the server returns the following body for call to url 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy/files/fooaktb2fvbvdkarwm2crklmei/content'
        """
        """
    When the user requests file with content path '/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy/files/fooaktb2fvbvdkarwm2crklmei/content'
    Then the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy/files/fooaktb2fvbvdkarwm2crklmei/content'
